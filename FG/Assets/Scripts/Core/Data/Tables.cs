namespace ForestGuild.Core.Data
{
    /// <summary>Все справочники одним объектом. Создаётся один раз на старте.</summary>
    public sealed class Tables
    {
        public Tuning Tuning;
        public Table<Zone> Zones;
        public Table<Item> Items;
        public Table<ZoneResource> ZoneResources;
        public Table<Beast> Beasts;
        public Table<ZoneBeast> ZoneBeasts;
        public Table<Drop> Drops;
        public Table<ZoneLore> ZoneLore;
        public Table<Skill> Skills;
        public Table<Trait> Traits;
        public Table<Profession> Professions;
        public Table<Person> Roster;
        public Table<Station> Stations;
        public Table<Recipe> Recipes;
        public Table<RecipeQuality> RecipeQuality;

        /// <summary>Эпизоды мира: с чего начинаем и с какими входными данными.</summary>
        public Table<Scenario> Scenarios;
        public Table<ScenarioParty> ScenarioParties;
    }
}
