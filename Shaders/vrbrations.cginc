float4 TransformToScreenPixelRight(float2 uv) {
#if defined(USING_STEREO_MATRICES)
	return float4(0, 0, 0, 0);
#else
	float2 sensorSize = 2 * float2(SENSOR_WIDTH * 3, SENSOR_HEIGHT) * float2(_ScreenParams.z - 1, _ScreenParams.w - 1);
	float2  p = uv * sensorSize - 1.0; //Scaling sensor
	float4 pos = float4(-p.x, p.y, 0, 1); //moving to pixel position
	pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
	return pos;
#endif
}

float4 TransformToScreenPixel(float2 uv) {
#if defined(USING_STEREO_MATRICES)
	return float4(0, 0, 0, 0);
#else
	float2 sensorSize = 2 * float2(SENSOR_WIDTH * 3, SENSOR_HEIGHT) * float2(_ScreenParams.z - 1, _ScreenParams.w - 1);
	float2  p = uv * sensorSize - 1.0; //Scaling sensor
	float4 pos = float4(p.x + sensorSize.x * _pixelPosition.x, p.y + sensorSize.y * _pixelPosition.y, 0, 1); //moving to pixel position
	pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
	return pos;
#endif
}

float3 EncodeFloat(int x, int y, float value, int2 floatIndex, int subPixel) {
	int negative = value < 0;
	value = abs(value);
	int i = int(saturate(value) * 255);
	float3 r = (float3)0;
	r += (subPixel == 0) * float3(negative, (128 & i) == 128, (64 & i) == 64);
	r += (subPixel == 1) * float3((32 & i) == 32, (16 & i) == 16, (8 & i) == 8);
	r += (subPixel == 2) * float3((4 & i) == 4, (2 & i) == 2, (1 & i) == 1);
	return (floatIndex.x == x) * (floatIndex.y == y) * r;
}

float3 EncodeShort(int x, int y, int i, int2 floatIndex, int subPixel) {
	float3 r = (float3)0;
	int negative = i < 0;
	i = abs(i);
	r += (subPixel == 0) * float3(negative, (128 & i) == 128, (64 & i) == 64);
	r += (subPixel == 1) * float3((32 & i) == 32, (16 & i) == 16, (8 & i) == 8);
	r += (subPixel == 2) * float3((4 & i) == 4, (2 & i) == 2, (1 & i) == 1);
	return (floatIndex.x == x) * (floatIndex.y == y) * r;
}

float3 EncodeInt(int x, int y, int i, int2 floatIndex, int subPixel) {
	float3 r = (float3)0;
	int negative = i < 0;
	i = abs(i);
	r += (floatIndex.x == x) * (floatIndex.y == y) * (subPixel == 0) * float3(negative, 0, (32768 & i) == 32768);
	r += (floatIndex.x == x) * (floatIndex.y == y) * (subPixel == 1) * float3((16384 & i) == 16384, (8192 & i) == 8192, (4096 & i) == 4096);
	r += (floatIndex.x == x) * (floatIndex.y == y) * (subPixel == 2) * float3((2048 & i) == 2048, (1024 & i) == 1024, (512 & i) == 512);

	r += (floatIndex.x == x + 1) * (floatIndex.y == y) * (subPixel == 0) * float3((256 & i) == 256, (128 & i) == 128, (64 & i) == 64);
	r += (floatIndex.x == x + 1) * (floatIndex.y == y) * (subPixel == 1) * float3((32 & i) == 32, (16 & i) == 16, (8 & i) == 8);
	r += (floatIndex.x == x + 1) * (floatIndex.y == y) * (subPixel == 2) * float3((4 & i) == 4, (2 & i) == 2, (1 & i) == 1);
	return r;
}

float3 EncodeBool(int x, int y, float sub, bool i, int2 floatIndex, int subPixel) {
	float3 r = (float3)0;
	r += (float3)((1 & i) == 1);
	return (floatIndex.x == x) * (floatIndex.y == y) * (subPixel == sub) * r;
}