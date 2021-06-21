#define PIXEL_WIDTH 0.5
#define PIXEL_HEIGHT 0.5

#define SENSOR_WIDTH 4
#define SENSOR_HEIGHT 5

float4 TransformToScreenMain (float2 uv) {
#if defined(USING_STEREO_MATRICES)
	return float4(0, 0, 0, 0);
#else
	float4 pos = float4(uv.xy * float2(2, 2) + float2(-1, -1), 0, 1);; //moving to pixel position
	pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
	return pos;
#endif
}

float4 TransformToScreenSensor(float2 uv) {
#if defined(USING_STEREO_MATRICES)
	return float4(0, 0, 0, 0);
#else
	float2 sensorSize = float2(SENSOR_WIDTH, SENSOR_HEIGHT) * float2(PIXEL_WIDTH, PIXEL_HEIGHT) / 50;
	float2  p = uv * sensorSize - 1.0; //Scaling sensor
	float4 pos = float4(p.x + sensorSize.x * _pixelPosition.x, p.y + sensorSize.y * _pixelPosition.y, 0, 1); //moving to pixel position
	pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
	return pos;
#endif
}

float3 EncodeShort(int x, int y, int i, int2 pixelindex) {
	float3 r = (float3)0;
	int negative = i < 0;
	i = abs(i);
	return (pixelindex.x == x) * (pixelindex.y == y) * float3(negative, float((i >> 4) & (15)) / 15, float((i) & (15)) / 15);
}

float3 EncodeFloat(int x, int y, float value, int2 pixelindex) {
	int negative = value < 0;
	value = abs(value);
	int i = int(saturate(value) * 255);
	return (pixelindex.x == x) * (pixelindex.y == y) * float3(negative, float((i >> 4) & (15)) / 15, float((i) & (15)) / 15);
}

float3 EncodeInt(int x, int y, int i, int2 pixelindex) {
	float3 r = (float3)0;
	int negative = i < 0;
	i = abs(i);
	r += (pixelindex.x == x + 0) * (pixelindex.y == y) * float3(negative, 0, float((i >> 12) & (15)) / 15);
	r += (pixelindex.x == x + 1) * (pixelindex.y == y) * float3(float((i >> 8) & (15)) / 15, float((i >> 4) & (15)) / 15, float((i) & (15)) / 15);
	return r;
}

float3 EncodeBool(int x, int y, bool i, int2 pixelindex) {
	float3 r = (float3)0;
	r += (float3)((1 & i) == 1);
	return (pixelindex.x == x) * (pixelindex.y == y) * r;
}