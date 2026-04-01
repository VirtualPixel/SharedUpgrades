using System;

namespace SharedUpgradesPlus.Models
{
    public sealed class Upgrade(string Name) : IEquatable<Upgrade>
    {
        public string Name { get; } = Name;
        public string CleanName
        {
            get
            {
                return Name.StartsWith("playerUpgrade") 
                    ? Name["playerUpgrade".Length..]
                    : Name;
            }
        }
        public bool Equals(Upgrade? other) => other is not null && other.Name == Name;
        public override bool Equals(object? obj) => Equals(obj as Upgrade);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
