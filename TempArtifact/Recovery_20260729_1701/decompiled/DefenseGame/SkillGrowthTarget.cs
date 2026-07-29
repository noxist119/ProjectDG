using System;

namespace DefenseGame;

[Flags]
public enum SkillGrowthTarget
{
	None = 0,
	Power = 1,
	SecondaryPower = 2,
	Duration = 4,
	Radius = 8,
	HitCount = 0x10
}
