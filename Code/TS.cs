using Game.Utils;

namespace ShowMiners.Constants {
    public static class TS {
        public static string Translate(string label) {
            return label.T();
        }

        public static string Truncate(string value, int maxLength) {
            const string ellipsis = "...";

            if (string.IsNullOrEmpty(value) || maxLength <= ellipsis.Length)
                return value;

            if (value.Length <= maxLength)
                return value;

            var cutPoint = maxLength - ellipsis.Length;
            var trimmed = value.Substring(0, cutPoint);

            return trimmed + ellipsis;
        }

        public static string UITitle = "showminers.ui.title";
        public static string RetrieveMiner = "showminers.ui.retrieveMiner";
        public static string SendMiner = "showminers.ui.sendMiner";
        public static string SentMiner = "showminers.ui.sentMiner";
        public static string ProgressMiner = "showminers.ui.progressMiner";
        public static string ShowDrills = "showminers.ui.showDrills";
        public static string KnownDrills = "showminers.ui.knownDrills";
        public static string InSector = "showminers.ui.inSector";
        public static string Difficulty = "showminers.ui.difficulty";
    }
}