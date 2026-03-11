using UnityEngine;
using UnityEngine.UIElements;

    public static class Utils
    {
        public static string FormatTimeToHours(float time)
        {
            int hours = Mathf.RoundToInt(time / 3600f);
            int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
        public static string FormatTimeToMinutes(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        public static float CalculateWPM(int validWordsSubmitted, int totalCharsTyped, int uncorrectedMistakes, int backspaces, float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f || totalCharsTyped <= 0) return 0f;

            const float BackspacePenaltyWeight = 0.1f; // Backspacing is corrective, not a full error

            float minutes = elapsedSeconds / 60f;

            // --- Track 1: Gross WPM from raw keystrokes (character-based standard formula) ---
            float grossWpm = totalCharsTyped / minutes;

            // --- Track 2: Direct WPM from validated word submissions ---
            float directWpm = validWordsSubmitted / minutes;

            // --- Blend: weight direct WPM more heavily since it's ground truth ---
            float blendedWpm = (directWpm * 0.7f) + (grossWpm * 0.3f);

            // --- Penalty: uncorrected mistakes are full errors; backspaces are minor ---
            float errorPenalty = uncorrectedMistakes / minutes;
            float backspacePenalty = (backspaces * BackspacePenaltyWeight) / minutes;

            float netWpm = blendedWpm - errorPenalty - backspacePenalty;

            return Mathf.Max(0f, netWpm);
        }
        /// <summary>
        /// Returns WPM formatted as a string with one decimal place.
        /// </summary>
        public static string FormatWPM(float wpm)
        {
            return wpm.ToString("F1") + " WPM";
        }

        /// <summary>
        /// Between 0-1f
        /// </summary>
        /// <param name="dB"></param>
        /// <returns></returns>
        public static float DecibelToFloat(float dB)
        {
            return Mathf.Pow(10f, dB / 20f);
        }
        public static float FloatToDecibel(float value)
        {
            return Mathf.Log10(value) * 20f;
        }
    }