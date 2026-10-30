using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    [UxmlElement]
    public partial class SpinnerLoadingButton : StatusButton
    {
        const float DegreesPerMs = 0.36f; // 360 per second
        const string Spinner = "spinner";
        const string ButtonSpinnerClass = "menu-button__spinner";

        readonly VisualElement spinner;
        readonly IVisualElementScheduledItem spin;
        float angle;

        public SpinnerLoadingButton()
        {
            spinner = new VisualElement { name = Spinner, pickingMode = PickingMode.Ignore };
            spinner.AddToClassList(ButtonSpinnerClass);
            Add(spinner);

            spin = spinner.schedule.Execute(state =>
            {
                angle = (angle + state.deltaTime * DegreesPerMs) % 360f;
                spinner.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            }).Every(16);
            spin.Pause();
        }

        protected void OnStatusChanged(ButtonStatus previousStatus, ButtonStatus newStatus)
        {
            if (newStatus == ButtonStatus.Loading)
                spin.Resume();
            else
                spin.Pause();
        }
    }
}

