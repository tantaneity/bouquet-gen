public enum Species
{
    Rose = 0,
    OpenBloom = 1,
    Anemone = 2,
    Dahlia = 3,
    Carnation = 4,
    BerryCluster = 5,
    Lavender = 6,
    Gypsophila = 7,
    Eucalyptus = 8,
    Fern = 9,
    LongBlade = 10,
    LeafSprig = 11
}

public static class SpeciesTraits
{
    public static float TipReserve(this Species species)
    {
        switch (species)
        {
            case Species.Lavender: return 3.0f;
            case Species.Gypsophila: return 2.0f;
            case Species.Eucalyptus: return 3.4f;
            case Species.Fern: return 2.9f;
            case Species.LongBlade: return 3.2f;
            case Species.LeafSprig: return 2.8f;
            default: return 0.0f;
        }
    }

    // how far a spray's leaves reach sideways from its rib, in head sizes
    public static float SprayRadius(this Species species)
    {
        switch (species)
        {
            case Species.Lavender: return 0.20f;
            case Species.Gypsophila: return 1.0f;
            case Species.Eucalyptus: return 0.45f;
            case Species.Fern: return 0.45f;
            case Species.LongBlade: return 0.5f;
            case Species.LeafSprig: return 0.40f;
            default: return 0.0f;
        }
    }

    public static bool IsBloom(this Species species)
    {
        return species <= Species.BerryCluster;
    }
}
