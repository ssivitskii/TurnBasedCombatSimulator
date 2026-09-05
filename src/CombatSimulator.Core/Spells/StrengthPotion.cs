using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Spells;

public sealed class StrengthPotion : ISpell
{
    public ICreature Apply(ICreature target)
    {
        target.SetAttack(new AttackPoint((int)Math.Min(int.MaxValue, (long)target.Attack.Value + 5)));
        return target;
    }
}
