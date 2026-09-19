public static class BouquetHash
{
    public static float Unit(int index, int channel, int seed)
    {
        unchecked
        {
            uint value = (uint)(index * 7 + channel * 131 + seed * 977 + 11);
            value = (value << 13) ^ value;
            value = value * (value * value * 15731u + 789221u) + 1376312589u;
            return (value & 0x7fffffffu) / (float)0x7fffffff;
        }
    }
}
