using UnityEditor;
namespace MamasComing.Editor
{
    public static class MamaHouseStudyBuilder
    {
        public const string ScenePath = "Assets/MamasComing/Scenes/MamaHouseStudy.unity";
        [MenuItem("Mama's Coming/House Study/Build Separate Two Storey Scene")]
        public static void Build() => MamaHomeV2Builder.Build();
    }
}
