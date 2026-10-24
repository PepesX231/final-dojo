namespace MamasComing.Editor
{
    // Compatibility entry points now target the revised roofed house and 50-task review.
    public static class MamaHouseStudyReview
    {
        public static void StartFromEditor() => MamaHouseRevisionReview.StartFromEditor();
        public static void Run() => MamaHouseRevisionReview.Run();
    }
}
