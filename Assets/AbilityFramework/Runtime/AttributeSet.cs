using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityAbilityKit
{
    public enum AttributeId { Health, Mana, AttackPower, MoveSpeed }
    public enum ModifierOperation { Add, Multiply }

    public sealed class AttributeDefinition
    {
        public AttributeId Id { get; }
        public float Initial { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public AttributeDefinition(AttributeId id, float initial, float minimum, float maximum)
        {
            if (!Enum.IsDefined(typeof(AttributeId), id) || !Finite(initial) || !Finite(minimum) || !Finite(maximum) || minimum > maximum || initial < minimum || initial > maximum)
                throw new ArgumentException("Invalid attribute definition.");
            Id = id; Initial = initial; Minimum = minimum; Maximum = maximum;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
    public sealed class AttributeModifier
    {
        public AttributeId Attribute { get; }
        public ModifierOperation Operation { get; }
        public float Magnitude { get; }
        public AttributeModifier(AttributeId attribute, ModifierOperation operation, float magnitude)
        {
            if (!Enum.IsDefined(typeof(AttributeId), attribute) || !Enum.IsDefined(typeof(ModifierOperation), operation) || !AttributeDefinition.Finite(magnitude) || operation == ModifierOperation.Multiply && magnitude < 0)
                throw new ArgumentException("Invalid modifier.");
            Attribute = attribute; Operation = operation; Magnitude = magnitude;
        }
    }

    /// <summary>영구 기본값과 일시 modifier를 분리해 효과 만료 시 원래 값을 잘못 복원하지 않는다.</summary>
    public sealed class AttributeSet
    {
        private readonly Dictionary<AttributeId, AttributeDefinition> definitions;
        private readonly Dictionary<AttributeId, float> bases;
        private readonly Dictionary<long, AttributeModifier[]> modifiers = new Dictionary<long, AttributeModifier[]>();
        public AttributeSet(IEnumerable<AttributeDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var values = definitions.ToArray();
            if (values.Length == 0 || values.Any(d => d == null)) throw new ArgumentException("Attributes are required.");
            this.definitions = values.ToDictionary(d => d.Id);
            bases = values.ToDictionary(d => d.Id, d => d.Initial);
        }
        public static AttributeSet CreateDefault() => new AttributeSet(new[] {
            new AttributeDefinition(AttributeId.Health, 100, 0, 100), new AttributeDefinition(AttributeId.Mana, 100, 0, 100),
            new AttributeDefinition(AttributeId.AttackPower, 10, 0, 1000), new AttributeDefinition(AttributeId.MoveSpeed, 5, 0, 50) });
        public bool Contains(AttributeId id) => definitions.ContainsKey(id);
        public float GetBase(AttributeId id) => bases[id];
        public float Maximum(AttributeId id) => definitions[id].Maximum;
        private float Clamp(AttributeId id, double value) => (float)Math.Max(definitions[id].Minimum, Math.Min(definitions[id].Maximum, value));
        public void SetBase(AttributeId id, float value)
        {
            if (!AttributeDefinition.Finite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            bases[id] = Clamp(id, value);
        }
        public float Get(AttributeId id)
        {
            double addition = 0, product = 1;
            bool zeroMultiplier = false;
            foreach (var modifier in modifiers.Values.SelectMany(m => m).Where(m => m.Attribute == id))
                if (modifier.Operation == ModifierOperation.Add) addition += modifier.Magnitude;
                else if (modifier.Magnitude == 0) zeroMultiplier = true;
                else product *= modifier.Magnitude;
            double subtotal = bases[id] + addition;
            return Clamp(id, zeroMultiplier || subtotal == 0 ? 0 : subtotal * product);
        }
        internal void ApplyInstant(AttributeModifier modifier)
        {
            double value = bases[modifier.Attribute];
            bases[modifier.Attribute] = Clamp(modifier.Attribute, modifier.Operation == ModifierOperation.Add ? value + modifier.Magnitude : value * modifier.Magnitude);
        }
        internal bool CanSpend(AttributeId id, float amount) => Contains(id) && GetBase(id) - amount >= definitions[id].Minimum;
        internal void Spend(AttributeId id, float amount)
        {
            if (!CanSpend(id, amount)) throw new InvalidOperationException("Resource check must precede commit.");
            SetBase(id, GetBase(id) - amount);
        }
        internal void AddModifiers(long handle, IEnumerable<AttributeModifier> values) => modifiers.Add(handle, values.ToArray());
        internal void RemoveModifiers(long handle) => modifiers.Remove(handle);
    }
}
