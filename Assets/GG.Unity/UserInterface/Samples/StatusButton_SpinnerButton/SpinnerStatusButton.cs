using GG.Unity.UserInterface.Buttons;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// A StatusButton that shows a rotating spinner while Loading.
    /// Demonstrates the OnStatusChanged hook: USS shows/hides the spinner,
    /// this class only drives the rotation (USS can't loop an animation).
    /// </summary>
    [UxmlElement]
    public partial class SpinnerStatusButton : StatusButton
    {
        public const string SpinnerButtonClass = "spinner-status-button";
        public const string SpinnerClass = "status-button__spinner";

        private const float DegreesPerSecond = 360f;
        private const long FrameIntervalMs = 16;

        private readonly VisualElement spinner;
        private readonly IVisualElementScheduledItem spin;
        private float angle;

        public SpinnerStatusButton()
        {
            AddToClassList(SpinnerButtonClass);

            spinner = new VisualElement { name = "spinner", pickingMode = PickingMode.Ignore };
            spinner.AddToClassList(SpinnerClass);
            Add(spinner);

            spin = schedule.Execute(Rotate).Every(FrameIntervalMs);
            spin.Pause();

            // Don't tick while off-screen; resume correctly when (re)attached.
            RegisterCallback<AttachToPanelEvent>(_ => UpdateSpin());
            RegisterCallback<DetachFromPanelEvent>(_ => spin.Pause());
        }

        protected override void OnStatusChanged(ButtonStatus previous, ButtonStatus current) => UpdateSpin();

        private void UpdateSpin()
        {
            if (Status == ButtonStatus.Loading && panel != null)
                spin.Resume();
            else
                spin.Pause();
        }

        private void Rotate(TimerState timer)
        {
            angle = (angle + DegreesPerSecond * timer.deltaTime / 1000f) % 360f;
            spinner.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
        }
    }
}
