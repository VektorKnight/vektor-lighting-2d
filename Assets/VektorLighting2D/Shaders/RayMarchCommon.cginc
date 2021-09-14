static const float FMAX = 3.402823466E38;
static const float SQRT2 = 1.414214;
static const float EPSILON = 1.401298E-45;

// Types of lights targeted by a ray.
static const uint LIGHT_TYPE_POINT = 0;
static const uint LIGHT_TYPE_SPOT = 1;
static const uint LIGHT_TYPE_RECT = 2;
static const uint LIGHT_TYPE_POLY = 3;

// Types of shapes in the scene.
static const uint SHAPE_TYPE_CIRCLE = 0;
static const uint SHAPE_TYPE_RECT = 1;
static const uint SHAPE_TYPE_POLY = 2;

// TODO: This struct is becoming excessively large.
// Gotta split this thing up somehow and maybe remove unnecessary data.
struct Ray {
    uint id;                  // Pixel ID of the ray.
    float2 origin;            // Origin of the ray.
    float2 direction;         // Normalized direction of the ray.
    float3 color;             // Color the ray will contribute to the pixel if it reaches the target light.
    float light_distance;     // Distance ray must travel to hit the target light.
};

struct Circle {
    float2 position;
    float radius;
    uint enabled;
};

struct Rect {
    float2 position;
    float2 extents;
    uint enabled;
};

// TODO: Segment method helps with some things but also causes a lot of data duplication.
// Probably move this to just be indices into a vertex buffer like normal meshes.
struct Segment {
    float2 a;
    float2 b;
};

struct Polygon {
    uint offset;     // Starting offset in the segment buffer.
    uint length;     // Number of segments.
    uint enabled;
};

struct PointLight {
    float2 position;
    float3 color;
    float radius;
    uint enabled;
};

struct SpotLight {
    float2 position;
    float3 color;
    float radius;
    float4 cone;
    uint enabled;
};

struct PolygonLight {
    float3 color;
    float radius;
    uint offset;
    uint count;
    uint enabled;
};

float CircleSDF(const Circle circle, const float2 p) {
    return length(p - circle.position) - circle.radius;
}

float RectSDF(const Rect rect, const float2 p) {
    float2 d = abs(p - rect.position) - rect.extents;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float PolygonSDF(const uint offset, const uint count, const StructuredBuffer<Segment> segments, const float2 p) {
    const float2 v0 = segments[offset].a;
    float d = dot(p- v0, p - v0);
    float s = 1.0;
    for (uint i = offset; i < offset + count; i++) {
        const Segment seg = segments[i];
        // distance
        float2 e = seg.b - seg.a;
        float2 w = p - seg.a;
        float2 b = w - e*clamp( dot(w,e)/dot(e,e), 0.0, 1.0 );
        d = min( d, dot(b,b) );

        // winding number from http://geomalgorithms.com/a03-_inclusion.html
        const bool3 cond = bool3( p.y >= seg.a.y, 
                                  p.y < seg.b.y, 
                                  e.x * w.y > e.y * w.x );
        if( all(cond) || all(!cond) ) s = -s;  
    }
    
    return s*sqrt(d);
}

float Cross2D(const float2 a, const float2 b) {
    return a.x * b.y - b.x * a.y;
}

// Calculates the closest point to a line segment from a given point.
float2 PointAlongSegment(const Segment s, const float2 p) {
    const float sigma = clamp(dot(p - s.a, s.b - s.a) / dot(s.b - s.a, s.b - s.a), 0, 1);
    return s.a + sigma * (s.b - s.a);
}

// Calculates the center of mass between all points in a polygon.
float2 PolygonCenter(const Polygon polygon, const StructuredBuffer<Segment> segments) {
    float2 sum = float2(0,0);
    uint total = 0;
    for (uint i = polygon.offset; i < polygon.offset + polygon.length; i++) {
        const Segment segment = segments[i];
        sum += segment.a + segment.b;
        total += 2;
    }

    return sum / total;
}

// Calculates the closest point on a polygon from a given point.
float2 ClosestPointOnPolygon(const uint offset, const uint count, const StructuredBuffer<Segment> segments, const float2 p) {
    float2 nearest = float2(1, 1);
    float shortest = FMAX;
    for (uint i = offset; i < offset + count; i++) {
        const Segment segment = segments[i];

        // Get point on segment and update min.
        const float2 segment_point = PointAlongSegment(segment, p);
        const float segment_dist = length(segment_point - p);

        if (segment_dist < shortest) {
            shortest = segment_dist;
            nearest = segment_point;
        }
    }

    return nearest;
}

// Whether or not a vector (v) is between a and b.
// Used for spot lights.
bool IsVectorBetween(const float2 a, const float2 b, const float2 v) {
    return Cross2D(a, v) * Cross2D(a, b) <= 0.0 && Cross2D(b, v) * Cross2D(b, a) <= 0.0;
}

float InvLerp(const float x, const float a, const float b) {
    return (x - a) / (b - a);
}

// Adds RGB values to a packed RGBA32 value.
uint AddPacked(const uint a, const uint3 b) {
    // Unpack the first value.
    uint ar = (a & 0xFF);
    uint ag = (a >> 8 & 0xFF);
    uint ab = (a >> 16 & 0xFF);

    ar = clamp(ar + b.r, 0, 255);
    ag = clamp(ag + b.g, 0, 255);
    ab = clamp(ab + b.b, 0, 255);
    
    return ar | ag << 8 | ab << 16;
}

float2 ScreenToWorld(const float4x4 inv_world, const float4x4 inv_proj, const float2 screen, const float2 p) {
    const float4 clip = float4((p.x * 2.0 / screen.x) - 1.0, (p.y * 2.0 / screen.y) - 1.0, 0.0, 1.0);
    float4 view = mul(inv_proj, clip);
    view /= view.w;
    
    const float4 world = mul(inv_world, view);

    return world.xy;
}