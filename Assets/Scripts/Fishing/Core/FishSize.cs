using System;

namespace FishSizeNamespace
{
    public enum ESize
    {
        None,
        Tiny,
        Small,
        Medium,
        Large
    }

    public struct FishSize
    {
        public readonly ESize size;

        // Constructor
        public FishSize(ESize? size) =>
            this.size = size.GetValueOrDefault(ESize.None);
        
        // FishSize <-> Size (implicit)
        public static implicit operator FishSize(ESize? size) => 
            new FishSize(size);
        public static implicit operator ESize(FishSize fishSize) =>
            fishSize.size;

        // FishSize <-> int (explicit)
        public static explicit operator FishSize(int value) =>
            new FishSize((ESize)value);
        public static explicit operator int(FishSize fishSize) =>
            (int)fishSize.size;

        // FishSize -> bool (implicit)
        public static implicit operator bool(FishSize fishSize) => 
            fishSize.size != ESize.None;

        // FishSize -> string (explicit)
        public static explicit operator string(FishSize fishSize) => 
            Enum.GetName(typeof(ESize), fishSize.size);
        public override string ToString() => 
            Enum.GetName(typeof(ESize), size) ?? "None";
        
        // Equality operators
        public static FishSize operator ++(FishSize fishSize)
        {
            switch (fishSize.size)
            {
                case ESize.None:
                    return new FishSize(ESize.None);
                case ESize.Large:
                    return new FishSize(ESize.None);
                default:
                    return new FishSize(fishSize.size + 1);
            }
        }

        public static bool operator ==(FishSize left, FishSize right) =>
            left.size == right.size;
        public static bool operator !=(FishSize left, FishSize right) => 
            left.size != right.size;

        public override bool Equals(object obj) =>
            obj is FishSize other && this == other;

        public bool Equals(FishSize other) => size == other.size;
        public override int GetHashCode() => size.GetHashCode();

    }
}