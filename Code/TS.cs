using Game.Utils;

namespace ShowMiners.Constants {
    public static class TS {
        public static string Translate(string label) {
            return label.T();
        }

        public static string UITitle = "showminers.ui.title";
        public static string RetrieveMiner = "showminers.ui.retrieveMiner";
    }
}