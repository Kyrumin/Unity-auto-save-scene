using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kyrumin 
{
    public class AutoSaveConfig : ScriptableObject 
    {
        [Tooltip("Включить функцию автоматического сохранения")]
        public bool AutoSaveEnabled;

        [Tooltip("Частота автоматической активации сохранения в минутах"),  Min(0.1f)]
        public float AutoSaveFrequency = 1f;

        [Tooltip("Отправлять сообщения каждый раз, когда сцена автоматически сохраняется")]
        public bool Logging;
    }


    [CustomEditor(typeof(AutoSaveConfig))]
    public class KyruminAutoSave : Editor 
    {
        private static AutoSaveConfig autoSaveConfig;
        private static CancellationTokenSource tokenSource;
        private static Task autoSaveTask;

        [InitializeOnLoadMethod]
        private static void OnInitialize() 
        {
            FetchConfig();
            CancelTask();

            tokenSource = new CancellationTokenSource();
            autoSaveTask = SaveInterval(tokenSource.Token);
        }

        // Метод для загрузки конфигурационного файла
        private static void FetchConfig() 
        {
            while (true) 
            {
                if (autoSaveConfig != null) return;

                var path = GetConfigPath();

                if (path == null) 
                {
                    // Создание конфигурационного файла, если его нет
                    AssetDatabase.CreateAsset(CreateInstance<AutoSaveConfig>(), $"Assets/{nameof(AutoSaveConfig)}.asset");
                    Debug.Log("В корне вашего проекта создан конфигурационный файл.<b> Вы можете переместить его куда угодно.</b>");
                    continue;
                }

                autoSaveConfig = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path);

                break;
            }
        }

        // Метод для получения пути к конфигурационному файлу
        private static string GetConfigPath() 
        {
            var paths = AssetDatabase.FindAssets(nameof(AutoSaveConfig)).Select(AssetDatabase.GUIDToAssetPath).Where(c => c.EndsWith(".asset")).ToList();
            if (paths.Count > 1) Debug.LogWarning("Найдено несколько ресурсов конфигурации с автоматическим сохранением. Удалите один из них.");
            return paths.FirstOrDefault();
        }

        // Метод для отмены задачи сохранения
        private static void CancelTask() 
        {
            if (autoSaveTask == null) return;
            tokenSource.Cancel();
            autoSaveTask.Wait();
        }

        // Метод для установки интервала автоматического сохранения
        private static async Task SaveInterval(CancellationToken token) 
        {
            while (!token.IsCancellationRequested) 
            {
                await Task.Delay(Mathf.RoundToInt(autoSaveConfig.AutoSaveFrequency * 1000 * 60), token);
                if (autoSaveConfig == null) FetchConfig();

                // Проверки перед автоматическим сохранением
                if (!autoSaveConfig.AutoSaveEnabled || Application.isPlaying || BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) continue;
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) continue;

                // Автоматическое сохранение открытых сцен
                EditorSceneManager.SaveOpenScenes();
                if (autoSaveConfig.Logging) Debug.Log($"Сцена автоматически сохранена в {DateTime.Now:HH:mm:ss}");
            }
        }
        
        // Метод для отображения пути к конфигурационному файлу в редакторе
        [MenuItem("Window/Auto-save/Find config")]
        public static void ShowConfig() 
        {
            FetchConfig();

            var path = GetConfigPath();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path).GetInstanceID());
        }

        // Переопределенный метод для отображения дополнительной информации в редакторе
        public override void OnInspectorGUI() 
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Вы можете переместить этот ассет, куда угодно.", MessageType.Info);
        }
    }
}