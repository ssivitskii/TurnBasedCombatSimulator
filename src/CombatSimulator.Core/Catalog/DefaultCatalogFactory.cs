using CombatSimulator.Core.Creatures;

namespace CombatSimulator.Core.Catalog;

public static class DefaultCatalogFactory
{
    public static CreatureCatalog Create()
    {
        var catalog = new CreatureCatalog();
        catalog.Register("AmuletMaster", new CreatureFactory(() => new AmuletMaster()));
        catalog.Register("BattleAnalyst", new CreatureFactory(() => new BattleAnalyst()));
        catalog.Register("DeathlessHorror", new CreatureFactory(() => new DeathlessHorror()));
        catalog.Register("MimicChest", new CreatureFactory(() => new MimicChest()));
        catalog.Register("ViciousBrawler", new CreatureFactory(() => new ViciousBrawler()));
        return catalog;
    }
}
