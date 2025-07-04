/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#ifndef BITMAP_MUZZLE_FLAME_H
#define BITMAP_MUZZLE_FLAME_H

#include "Sim/Projectiles/Projectile.h"

class CColorMap;
struct AtlasedTexture;

class CBitmapMuzzleFlame : public CProjectile
{
	CR_DECLARE_DERIVED(CBitmapMuzzleFlame)

public:
	CBitmapMuzzleFlame();

	void Serialize(creg::ISerializer* s);

	void Draw() override;
	void Update() override;

	void Init(const CUnit* owner, const float3& offset) override;

	int GetProjectilesCount() const override;

	static bool GetMemberInfo(SExpGenSpawnableMemberInfo& memberInfo);

private:
	AtlasedTexture* sideTexture;
	AtlasedTexture* frontTexture;

	CColorMap* colorMap;

	float size;
	float length;
	float sizeGrowth;
	float frontOffset;
	float particleSpeed;
	float particleSpeedSpread;
	float airdrag;
	float3 gravity;
	int ttl;
	bool drawSideX;
	bool drawSideY;

	float invttl;
};

#endif // BITMAP_MUZZLE_FLAME_H
