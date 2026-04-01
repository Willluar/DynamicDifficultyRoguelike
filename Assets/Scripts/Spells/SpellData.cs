using UnityEngine;

public enum DamageType
{
    Melee,
    Fire,
    Ice,
    Lightning
}

public enum SpellType
{
    IceBolt,
    LightningBolt,
    Fireball
}

[System.Serializable]
public class SpellData
{
    public string spellName;
    public SpellType spellType;
    public DamageType damageType;

    public int damage;
    public int range;

    public int splashRadius;
    public int chainRange;
    public int chainCount;

    public GameObject projectilePrefab;
    public float projectileTravelTime = 0.2f;

    public GameObject impactEffectPrefab;
    public GameObject secondaryEffectPrefab;
}