using CombatSimulator.Core.Catalog;

namespace CombatSimulator.Application;

public static class BattleCatalog
{
    public static IReadOnlyList<string> Creatures => DefaultCatalogFactory.CreatureNames;

    public static IReadOnlyList<string> Modifiers { get; } = ["MagicShield", "DoubleStrike"];
}
