using System;
using System.Collections.Generic;

namespace Rotatonator
{
    public class RaidTargetInfo
    {
        public string Name { get; set; } = "";
        public int BaseTimerSeconds { get; set; }
        public double Hours => Math.Round(BaseTimerSeconds / 3600.0, 2);
        public double Days => Math.Round(BaseTimerSeconds / 86400.0, 2);
        public string FormattedDuration
        {
            get
            {
                if (Days >= 1.0)
                    return $"{Days:0.##} days ({Hours:0.#}h)";
                return $"{Hours:0.#} hours";
            }
        }
    }

    /// <summary>
    /// Database of raid target respawn / lockout times derived from Quarm server database.
    /// </summary>
    public static class RaidTargetDatabase
    {
        private static readonly Dictionary<string, RaidTargetInfo> Targets = new(StringComparer.OrdinalIgnoreCase);

        public static int TotalTargets => Targets.Count;

        static RaidTargetDatabase()
        {
            AddTarget("A Lava Defender", 496800);
            AddTarget("A Sky Defender", 496800);
            AddTarget("A burrower parasite", 237800);
            AddTarget("A mix", 1592000);
            AddTarget("Aaryonar", 583200);
            AddTarget("Adjutant D`kan", 64800);
            AddTarget("Advisor C`zatl", 64800);
            AddTarget("Ajorek the Crimson Fang", 496800);
            AddTarget("Amcilla", 3600);
            AddTarget("An Emerald Defender", 496800);
            AddTarget("An Iksar Trustee", 1592000);
            AddTarget("An Onyx Defender", 496800);
            AddTarget("Ancient Guardian", 1592000);
            AddTarget("Arch Inspector Nibi`zi", 1592000);
            AddTarget("Arch Lich Rhag`Zadune", 583200);
            AddTarget("Ashenbone Broodmaster", 237600);
            AddTarget("Aten Ha Ra", 583200);
            AddTarget("Avatar of Abhorrence", 237600);
            AddTarget("Battle Master Ska`tu", 1592000);
            AddTarget("Bazzt Zzzt", 64800);
            AddTarget("Beldion Icewind", 496800);
            AddTarget("Belijor the Emerald Eye", 496800);
            AddTarget("Bloodmaw", 60);
            AddTarget("BouncerMan", 583200);
            AddTarget("Brigadier G`tav", 64800);
            AddTarget("Bryrym", 496800);
            AddTarget("Captain Di`ouz", 1592000);
            AddTarget("Carx`Vean", 496800);
            AddTarget("Casalen", 64800);
            AddTarget("Cazic Thule", 583200);
            AddTarget("Cekenar", 583200);
            AddTarget("Clanging sounds", 1592000);
            AddTarget("Coercer Q`ioul", 64800);
            AddTarget("Coercer T`vala", 237600);
            AddTarget("Corrupter of Life", 237600);
            AddTarget("Dagarn the Destroyer", 583200);
            AddTarget("Dain Frostreaver IV", 583200);
            AddTarget("Deathfang", 1592000);
            AddTarget("Derakor the Vindicator", 237600);
            AddTarget("Di`zok elite guard", 1592000);
            AddTarget("Di`zok royal guard", 1592000);
            AddTarget("Diabo Xi Va", 583200);
            AddTarget("Diabo Xi Va Temariel", 583200);
            AddTarget("Diabo Xi Xin", 583200);
            AddTarget("Diabo Xi Xin Thall", 583200);
            AddTarget("Doomshade", 237800);
            AddTarget("Dozekar the Cursed", 237600);
            AddTarget("Dread", 64800);
            AddTarget("Drill Master Dih`roul", 1592000);
            AddTarget("Drusella Sathir", 86400);
            AddTarget("Druushk", 237600);
            AddTarget("Eashen of the Sky", 583200);
            AddTarget("Emperor Ssraeshza", 583200);
            AddTarget("Essedera", 64800);
            AddTarget("Eye of Veeshan", 64800);
            AddTarget("Faydedar", 237600);
            AddTarget("Foreman Ku`lul", 1592000);
            AddTarget("Foreman Mirt`akk", 1592000);
            AddTarget("Fright", 64800);
            AddTarget("Gangel", 3600);
            AddTarget("Garudon the Reviled", 237600);
            AddTarget("Gorenaire", 237600);
            AddTarget("Gorgalosk", 64800);
            AddTarget("Gozzrem", 583200);
            AddTarget("Gra`Vloren", 496800);
            AddTarget("Grand Advisor Zum`uul", 1592000);
            AddTarget("Grand Herbalist Mak`ha", 1592000);
            AddTarget("Grand Lorekeeper Kino Shai`din", 1592000);
            AddTarget("Grandmaster R`tal", 237600);
            AddTarget("Grave Master Zo`lun", 1592000);
            AddTarget("Grieg Veneficus", 583200);
            AddTarget("Grozzmel", 64800);
            AddTarget("Haggle Baron Dry`dn", 1592000);
            AddTarget("Hand of the Maestro", 237600);
            AddTarget("Herald Telcha", 1592000);
            AddTarget("Herbalist Apprentice", 1592000);
            AddTarget("High Priest M`kari", 237600);
            AddTarget("High Priest of Ssraeshza", 583200);
            AddTarget("Hoshkar", 237600);
            AddTarget("Hraashna the Warder", 583200);
            AddTarget("Hsrek", 496800);
            AddTarget("Ikatiar the Venom", 583200);
            AddTarget("Innoruuk", 583200);
            AddTarget("Interrogator Gi`mok", 1592000);
            AddTarget("Ioltos V`ghera", 64800);
            AddTarget("Jorlleag", 583200);
            AddTarget("Kaas Thox Xi Ans Dyek", 583200);
            AddTarget("Kaas Thox Xi Aten Ha Ra", 583200);
            AddTarget("Kal`Vunar", 496800);
            AddTarget("Kedrak", 496800);
            AddTarget("Keeper of Souls", 64800);
            AddTarget("Keldor Dek`Torek", 259200);
            AddTarget("Kelorek`Dar", 583200);
            AddTarget("Kennel Master Al`ele", 1592000);
            AddTarget("King Tormax", 583200);
            AddTarget("King Tranix", 64800);
            AddTarget("Klandicar", 237600);
            AddTarget("Korocust", 1592000);
            AddTarget("Korucust`s Courier", 1592000);
            AddTarget("Krigara", 64800);
            AddTarget("Lady Mirenilla", 583200);
            AddTarget("Lady Nevederia", 583200);
            AddTarget("Lady Vox", 583200);
            AddTarget("Lcea Katta", 237800);
            AddTarget("Lendiniara the Keeper", 583200);
            AddTarget("Lepethida", 64800);
            AddTarget("Lhranc", 900);
            AddTarget("Lodizal", 64800);
            AddTarget("Lord Feshlak", 583200);
            AddTarget("Lord Inquisitor Seru", 583200);
            AddTarget("Lord Koi`Doken", 583200);
            AddTarget("Lord Kreizenn", 583200);
            AddTarget("Lord Nagafen", 583200);
            AddTarget("Lord Vyemm", 583200);
            AddTarget("Lord Yelinak", 583200);
            AddTarget("Lord of Ire", 237600);
            AddTarget("Lord of Loathing", 237600);
            AddTarget("Loremaster Piza`tak", 1592000);
            AddTarget("Lurian", 496800);
            AddTarget("Maestro of Rancor", 237600);
            AddTarget("Magi P`tasa", 237600);
            AddTarget("Magi Rokyl", 64800);
            AddTarget("Magus Rokyl", 64800);
            AddTarget("Master Yael", 237600);
            AddTarget("Master of Spite", 237600);
            AddTarget("Master of the Guard", 583200);
            AddTarget("Mayong Mistmoore", 237600);
            AddTarget("Mayor Gubbin the Fallen", 237600);
            AddTarget("Mazrien", 496800);
            AddTarget("Midayor", 64800);
            AddTarget("Mistress of Scorn", 237600);
            AddTarget("Myga", 3600);
            AddTarget("Nanzata the Warder", 583200);
            AddTarget("Nelaarn the Ebon Claw", 496800);
            AddTarget("Netherbian Swarmlord", 237800);
            AddTarget("Nexona", 237600);
            AddTarget("Niblek", 1592000);
            AddTarget("Nir`Tan", 496800);
            AddTarget("Noble Dojorn", 64800);
            AddTarget("Nortlav the Scalekeeper", 237600);
            AddTarget("Observer Aq`touz", 1592000);
            AddTarget("Onava", 3600);
            AddTarget("Overking Bathezid", 64800);
            AddTarget("Overseer Dal`guur", 1592000);
            AddTarget("Overseer of Air", 64800);
            AddTarget("Phara Dar", 237600);
            AddTarget("Phara Don`t", 237600);
            AddTarget("Phinigel Autropos", 64800);
            AddTarget("Praesertum Bikun", 583200);
            AddTarget("Praesertum Matpa", 583200);
            AddTarget("Praesertum Rhugol", 583200);
            AddTarget("Praesertum Vantorus", 583200);
            AddTarget("Praetorian Myral", 64800);
            AddTarget("Prince Selrach Di`zok", 64800);
            AddTarget("Protector of Sky", 64800);
            AddTarget("Queen Velazul Di`zok", 64800);
            AddTarget("Quoza", 3600);
            AddTarget("Rhag`Mozdezh", 583200);
            AddTarget("Rhag`Zhezum", 237800);
            AddTarget("Rhozth Ssrakezh", 583200);
            AddTarget("Royal Sarnak Herbalist", 1592000);
            AddTarget("Royal Scribe Kaavin", 237600);
            AddTarget("Rumblecrush", 237800);
            AddTarget("Sarnak Collective Auditor", 1592000);
            AddTarget("Scout Charisa", 900);
            AddTarget("Servitor of Luclin", 237800);
            AddTarget("Sevalak", 583200);
            AddTarget("Severilous", 237600);
            AddTarget("Shei Vinitras", 583200);
            AddTarget("Silverwing", 237600);
            AddTarget("Slyder the Ancient", 583200);
            AddTarget("Sontalak", 237600);
            AddTarget("Spirit of Radir", 12800);
            AddTarget("Spirit of Tawro", 12800);
            AddTarget("Ssraeshzian Blood Golem", 583200);
            AddTarget("Stormfeather", 64800);
            AddTarget("Talendor", 237600);
            AddTarget("Tasi V`ghera", 64800);
            AddTarget("Tavekalem", 64800);
            AddTarget("Telkorenar", 583200);
            AddTarget("Terror", 64800);
            AddTarget("Thall Va Kelun", 237800);
            AddTarget("Thall Va Xakra", 237800);
            AddTarget("Thall Xundraux Diabo", 583200);
            AddTarget("The Avatar of War", 583200);
            AddTarget("The Bridge Keeper", 1592000);
            AddTarget("The Burrower Beast", 237);
            AddTarget("The Final Arbiter", 583200);
            AddTarget("The Idol of Rallos Zek", 583200);
            AddTarget("The Insanity Crawler", 583000);
            AddTarget("The Itraer Vius", 237800);
            AddTarget("The Progenitor", 583200);
            AddTarget("The Statue of Rallos Zek", 583200);
            AddTarget("The Va`Dyn", 237800);
            AddTarget("The clang", 1592000);
            AddTarget("The cries", 1592000);
            AddTarget("The earth", 1592000);
            AddTarget("The musty", 1592000);
            AddTarget("The sounds", 1592000);
            AddTarget("The stench", 1592000);
            AddTarget("The thick", 1592000);
            AddTarget("Thought Horror Overfiend", 237800);
            AddTarget("Thylex of Veeshan", 583200);
            AddTarget("Trakanon", 237600);
            AddTarget("Trakanon the Blightfallen", 237600);
            AddTarget("Tukaarak the Warder", 583200);
            AddTarget("Tunare", 583200);
            AddTarget("Underboss Myli`ki", 1592000);
            AddTarget("Va Xi Aten Ha Ra", 583200);
            AddTarget("Vault Master Shu`zo", 1592000);
            AddTarget("Velketor the Sorcerer", 237600);
            AddTarget("Venril Sathir", 237600);
            AddTarget("Ventani the Warder", 583200);
            AddTarget("Vukuz", 496800);
            AddTarget("Vyzh`dra the Cursed", 583200);
            AddTarget("Vyzh`dra the Exiled", 583200);
            AddTarget("War Priestess T`zan", 64800);
            AddTarget("Warlord Skarlon", 64800);
            AddTarget("Watch Captain Hir`roul", 1592000);
            AddTarget("Watch Sergeant Riz`oul", 1592000);
            AddTarget("Wel`Wnas", 496800);
            AddTarget("Wraith of a Di`zok Hero", 1592000);
            AddTarget("Wraith of a Shissir", 3600);
            AddTarget("Wuoshi", 583200);
            AddTarget("Xerkizh The Creator", 583200);
            AddTarget("Xygoz", 237600);
            AddTarget("Yendilor the Cerulean Wing", 496800);
            AddTarget("Ymmeln", 64800);
            AddTarget("You hear", 1592000);
            AddTarget("You seem", 1592000);
            AddTarget("Your feet splash", 1592000);
            AddTarget("Your mind", 1592000);
            AddTarget("Your shin", 1592000);
            AddTarget("Zapho the Ancient", 583200);
            AddTarget("Zelnithak", 237800);
            AddTarget("Zlandicar", 237600);
            AddTarget("Zlexak", 583200);
            AddTarget("Zyerek Onyxblood", 496800);
            AddTarget("a Alchemist`s Acolyte", 1592000);
            AddTarget("a Block Foreman", 1592000);
            AddTarget("a Chokidai Bloodhound", 1592000);
            AddTarget("a Chokidai Bonesnapper", 1592000);
            AddTarget("a Chokidai Fleshripper", 1592000);
            AddTarget("a Chokidai Growler", 1592000);
            AddTarget("a Chokidai Herbsniffer", 1592000);
            AddTarget("a Chokidai Lasher", 1592000);
            AddTarget("a Chokidai Mangler", 1592000);
            AddTarget("a Chokidai Mauler", 1592000);
            AddTarget("a Chokidai Pitfighter", 1592000);
            AddTarget("a Chokidai Rootdigger", 1592000);
            AddTarget("a Chokidai Sniffer", 1592000);
            AddTarget("a Chokidai Sunderfiend", 1592000);
            AddTarget("a Chokidai Tearer", 1592000);
            AddTarget("a Chokidai Terror", 1592000);
            AddTarget("a Chokidai Wardog", 1592000);
            AddTarget("a Chokidai Whelp", 1592000);
            AddTarget("a Di`zok Aruspex", 1592000);
            AddTarget("a Di`zok Captain", 1592000);
            AddTarget("a Di`zok Channeler", 1592000);
            AddTarget("a Di`zok Conscript", 1592000);
            AddTarget("a Di`zok Cryptmaster", 1592000);
            AddTarget("a Di`zok Dragoon", 1592000);
            AddTarget("a Di`zok Follower", 1592000);
            AddTarget("a Di`zok Gravebinder", 1592000);
            AddTarget("a Di`zok Guardian", 1592000);
            AddTarget("a Di`zok Invoker", 1592000);
            AddTarget("a Di`zok Jannisary", 1592000);
            AddTarget("a Di`zok Legionnaire", 1592000);
            AddTarget("a Di`zok Librarian", 1592000);
            AddTarget("a Di`zok Myrmidon", 1592000);
            AddTarget("a Di`zok Partisan", 1592000);
            AddTarget("a Di`zok Recruit", 1592000);
            AddTarget("a Di`zok Revealer", 1592000);
            AddTarget("a Di`zok Sage", 1592000);
            AddTarget("a Di`zok Seer", 1592000);
            AddTarget("a Di`zok Soldier", 1592000);
            AddTarget("a Di`zok Underling", 1592000);
            AddTarget("a Di`zok Warlord", 1592000);
            AddTarget("a Dizok Herbalist", 1592000);
            AddTarget("a Dizok Observer", 1592000);
            AddTarget("a Dizok Researcher", 1592000);
            AddTarget("a Dizok Sergeant", 1592000);
            AddTarget("a Greater Guardian", 1592000);
            AddTarget("a Lesser Guardian", 1592000);
            AddTarget("a Reanimated Berserker", 1592000);
            AddTarget("a Reanimated Champion", 1592000);
            AddTarget("a Reanimated Conscript", 1592000);
            AddTarget("a Reanimated Dragoon", 1592000);
            AddTarget("a Reanimated Partisan", 1592000);
            AddTarget("a Royal Library Guard", 1592000);
            AddTarget("a Sarnak Dog Handler", 1592000);
            AddTarget("a Sarnak Foreman", 1592000);
            AddTarget("a Sarnak Herb Collector", 1592000);
            AddTarget("a Sarnak Manager", 1592000);
            AddTarget("a Sarnak Overseer", 1592000);
            AddTarget("a Sarnak Physician", 1592000);
            AddTarget("a Sarnak Slavedriver", 1592000);
            AddTarget("a Sarnak Slavemaster", 1592000);
            AddTarget("a Sarnak Underboss", 1592000);
            AddTarget("a Sarnak Whipcracker", 1592000);
            AddTarget("a Sarnak accoucheur", 1592000);
            AddTarget("a Sarnak aruspex", 1592000);
            AddTarget("a Sarnak myrmidon", 1592000);
            AddTarget("a Sarnak pitboss", 1592000);
            AddTarget("a Scarred Overseer", 1592000);
            AddTarget("a Shai`din Bloodomen", 1592000);
            AddTarget("a Shai`din Bonecollector", 1592000);
            AddTarget("a Shai`din Conscript", 1592000);
            AddTarget("a Shai`din Follower", 1592000);
            AddTarget("a Shai`din Hemlock", 1592000);
            AddTarget("a Shai`din Jannisary", 1592000);
            AddTarget("a Shai`din Nightfear", 1592000);
            AddTarget("a Shai`din Partisan", 1592000);
            AddTarget("a Shai`din Savant", 1592000);
            AddTarget("a Shai`din Soldier", 1592000);
            AddTarget("a Shai`din Soultrapper", 1592000);
            AddTarget("a Shai`din Warlord", 1592000);
            AddTarget("a Slaveblock Guard", 1592000);
            AddTarget("a Thulian High Ritualist", 583200);
            AddTarget("a Tortured Iksar Miner", 1592000);
            AddTarget("a Treasury Guard", 1592000);
            AddTarget("a Veteran Drillmaster", 1592000);
            AddTarget("a Veteran Overseer", 1592000);
            AddTarget("a Veteran Underboss", 1592000);
            AddTarget("a Wizened Herb Collector", 1592000);
            AddTarget("a black reaver", 900);
            AddTarget("a broken golem", 3600);
            AddTarget("a cerulean sky gazer", 496800);
            AddTarget("a chardoki golem", 1592000);
            AddTarget("a chokidai lacerator", 1592000);
            AddTarget("a chokidai mangler", 1592000);
            AddTarget("a chokidai sunderfiend", 1592000);
            AddTarget("a confused slave", 1592000);
            AddTarget("a crimson claw hatchling", 496800);
            AddTarget("a crystal shard golem", 1592000);
            AddTarget("a dirty slave", 1592000);
            AddTarget("a dracoliche", 237600);
            AddTarget("a dracolichen", 237600);
            AddTarget("a droopy slave", 1592000);
            AddTarget("a fiery watcher", 496800);
            AddTarget("a froglok slave", 1592000);
            AddTarget("a glimmer drake", 496800);
            AddTarget("a glimmering drake", 496800);
            AddTarget("a glyph covered serpent", 583200000);
            AddTarget("a gruff slave", 1592000);
            AddTarget("a grumpy slave", 1592000);
            AddTarget("a lava dancer", 496800);
            AddTarget("a miserable slave", 1592000);
            AddTarget("a muddy slave", 1592000);
            AddTarget("a ratman slave", 1592000);
            AddTarget("a restless burrower", 237800);
            AddTarget("a sad slave", 1592000);
            AddTarget("a sarnak accoucheur", 1592000);
            AddTarget("a sarnak arbiter", 1592000);
            AddTarget("a sarnak aruspex", 1592000);
            AddTarget("a sarnak aruspice", 1592000);
            AddTarget("a sarnak caedosaur", 1592000);
            AddTarget("a sarnak janissary", 1592000);
            AddTarget("a sarnak myrmidon", 1592000);
            AddTarget("a sarnak templar", 1592000);
            AddTarget("a shimmering green drake", 496800);
            AddTarget("a skinny slave", 1592000);
            AddTarget("a skyseeker hatchling", 496800);
            AddTarget("a soggy slave", 1592000);
            AddTarget("a terrified slave", 1592000);
            AddTarget("a thunder spirit princess", 64800);
            AddTarget("a tired slave", 1592000);
            AddTarget("an Ancient Guardian", 1592000);
            AddTarget("an Apprentice Herbalist", 1592000);
            AddTarget("an Enslaved Iksar Miner", 1592000);
            AddTarget("an Imperial Advisor", 1592000);
            AddTarget("an Imperial Courier", 1592000);
            AddTarget("an Imperial Defender", 1592000);
            AddTarget("an Imperial Gravemaster", 1592000);
            AddTarget("an Imperial Inspector", 1592000);
            AddTarget("an Imperial Interrogator", 1592000);
            AddTarget("an Imperial Officer", 1592000);
            AddTarget("an Imperial Wardog", 1592000);
            AddTarget("an accused traitor", 1592000);
            AddTarget("an ancient guardian", 496800);
            AddTarget("an ancient sky drake", 496800);
            AddTarget("an angry slave", 1592000);
            AddTarget("an apprentice kennelmaster", 1592000);
            AddTarget("an apprentice lorekeeper", 1592000);
            AddTarget("an ebon wing hatchling", 496800);
            AddTarget("an elder onyx drake", 496800);
            AddTarget("an emerald eye hatchling", 496800);
            AddTarget("an emerald sky defender", 496800);
            AddTarget("an enraged slave", 1592000);
            AddTarget("an escaped burrower", 64800);
            AddTarget("an escaped slave", 1592000);
            AddTarget("an iksar betrayer", 7200);
            AddTarget("an iksar collaborator", 1592000);
            AddTarget("an iksar slave", 1592000);
            AddTarget("an imperial construct", 1592000);
            AddTarget("an insane slave", 1592000);
            AddTarget("an off duty Accountant", 1592000);
            AddTarget("an off duty Overseer", 1592000);
            AddTarget("an off duty Pitboss", 1592000);
            AddTarget("an off duty Researcher", 1592000);
            AddTarget("an off duty Scribe", 1592000);
            AddTarget("an off duty Slavemaster", 1592000);
            AddTarget("an onyx sky drake", 496800);
            AddTarget("an unhappy slave", 1592000);
            AddTarget("judicator of Di`zok", 1592000);
            AddTarget("sorcerer of Di`zok", 1592000);
            AddTarget("the Hand of Veeshan", 64800);
            AddTarget("the ancient crawler", 583200);
        }

        private static void AddTarget(string name, int seconds)
        {
            var info = new RaidTargetInfo
            {
                Name = name,
                BaseTimerSeconds = seconds
            };

            string key = NormalizeKey(name);
            Targets[key] = info;

            // Also register without "the " prefix if applicable
            if (key.StartsWith("the "))
            {
                string stripped = key.Substring(4).Trim();
                if (!Targets.ContainsKey(stripped))
                {
                    Targets[stripped] = info;
                }
            }
        }

        public static string NormalizeKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return name
                .Replace('`', '\'')
                .Replace('_', ' ')
                .Trim()
                .ToLowerInvariant();
        }

        /// <summary>
        /// Attempts to find a raid target in the database.
        /// Handles case-insensitivity, apostrophes/backticks, and optional "The " prefixes.
        /// </summary>
        public static bool TryFindTarget(string mobName, out RaidTargetInfo target)
        {
            target = null!;
            if (string.IsNullOrWhiteSpace(mobName))
                return false;

            string key = NormalizeKey(mobName);

            // 1. Exact match
            if (Targets.TryGetValue(key, out var found))
            {
                target = found;
                return true;
            }

            // 2. Strip "The " prefix
            if (key.StartsWith("the ") && Targets.TryGetValue(key.Substring(4).Trim(), out found))
            {
                target = found;
                return true;
            }

            // 3. Try with "The " prefix
            if (!key.StartsWith("the ") && Targets.TryGetValue("the " + key, out found))
            {
                target = found;
                return true;
            }

            // 4. Substring search if key is long enough (> 4 chars)
            foreach (var kvp in Targets)
            {
                if (kvp.Key.Length > 4 && (key.Contains(kvp.Key) || kvp.Key.Contains(key)))
                {
                    target = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        public static int Count => Targets.Count;
    }
}
