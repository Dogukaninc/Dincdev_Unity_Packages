using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class Extensions
    {
        public static string FormatNumber(int value)
        {
            return value >= 1000 ? (value / 1000f).ToString("0.#") + "k" : value.ToString();
        }


        public static string FormatNumber(float value)
        {
            return value >= 1000 ? (value / 1000f).ToString("0.#") + "k" : value.ToString();
        }

        public static string GenerateHashedString(string str)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string GenerateUID(Transform transform)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return GenerateHashedString($"{sceneName}|{transform.position}|{transform.gameObject.name}");
        }

        public static string ColoredText(string v, Color color)
        {
            string colorCode = ColorUtility.ToHtmlStringRGB(color);
            return ($"<color=#{colorCode}>{v}</color>");
        }

        public static string ColorText(this string v, Color color)
        {
            string colorCode = ColorUtility.ToHtmlStringRGB(color);
            return ($"<color=#{colorCode}>{v}</color>");
        }

        public static void ColorLog(object msg, Color color)
        {
            string colorCode = ColorUtility.ToHtmlStringRGB(color);
            Debug.Log($"<color=#{colorCode}>{msg}</color>");
        }

        public static Vector3 ToVector3(this Vector2 vector2)
        {
            return new Vector3(vector2.x, vector2.y, 0);
        }

        public static Vector2 Random(this Vector2 vector2, float min, float max)
        {
            float randomX = UnityEngine.Random.Range(min, max);
            float randomY = UnityEngine.Random.Range(min, max);
            return new Vector2(randomX, randomY);
        }

        public static Vector3 RandomXY(this Vector3 vector2, float min, float max)
        {
            float randomX = UnityEngine.Random.Range(min, max) + vector2.x;
            float randomY = UnityEngine.Random.Range(min, max) + vector2.y;
            return new Vector2(randomX, randomY);
        }

        public static string ToTimeString(this float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int secondsRemaining = totalSeconds % 60;

            return $"{minutes:D2}:{secondsRemaining:D2}";
        }

        #region UniTask

        // public static UniTask CWaitForFixedSeconds(this UniTask task, float duration, bool ignoreTimeScale = false,
        //     PlayerLoopTiming delayTiming = PlayerLoopTiming.FixedUpdate,
        //     CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
        // {
        //     return UniTask.Delay(Mathf.RoundToInt(1000 * duration), ignoreTimeScale, delayTiming, cancellationToken,
        //         cancelImmediately);
        // }

        #endregion
    }
}