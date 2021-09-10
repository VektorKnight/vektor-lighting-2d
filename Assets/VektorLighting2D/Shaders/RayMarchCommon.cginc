struct Ray {
    uint id;            // Pixel ID of the ray.
    float2 origin;      // Origin of the ray.
    float2 direction;   // Normalized direction of the ray.
    float3 color;       // Color the ray will contribute to the pixel if it reaches the target light.
    float distance;     // Total distance travelled by this ray.
    
    uint light_id;
    float light_distance;     // Distance ray must travel to hit the target light.
};

struct PointLight {
    float2 position;
    float3 color;
    float radius;
};

struct SpotLight {
    
};

struct Circle {
    float2 position;
    float radius;
};

struct Rect {
    float2 position;
    float2 extents;
};

struct Segment {
    float2 a;
    float2 b;
    float2 normal;
};

struct Polygon {
    uint offset;    // Starting offset in the segment buffer.
    uint count;     // Number of segments.
};

float CircleSDF(const Circle circle, const float2 position) {
    return length(position - circle.position) - circle.radius;
}

float RectSDF(const Rect rect, const float2 position) {
    float2 d = abs(position - rect.position) - rect.extents;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

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