using Lykke.Bil2.SharedDomain;
using System;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class AssetInfo : IEquatable<AssetInfo>
    {
        public Asset Asset { get; }
        public int Scale { get; }

        public AssetInfo(Asset asset, int scale)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Scale = scale;
        }

        public bool Equals(AssetInfo other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Asset, other.Asset) && Scale == other.Scale;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((AssetInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Asset.GetHashCode();
                hashCode = (hashCode * 397) ^ Scale;
                return hashCode;
            }
        }
    }
}
