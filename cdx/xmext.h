#pragma once

inline XMVECTOR XM_CALLCONV _XMVector3Row(FXMVECTOR x, FXMVECTOR y, FXMVECTOR z)
{
  XMVECTOR t;
  t = _mm_shuffle_ps(x, y, _MM_SHUFFLE(1, 1, 0, 0)); //xxyy
  t = _mm_shuffle_ps(t, t, _MM_SHUFFLE(0, 0, 2, 0)); //xyxx
  t = _mm_shuffle_ps(t, z, _MM_SHUFFLE(3, 2, 1, 0)); //xyz0
  return t;
}