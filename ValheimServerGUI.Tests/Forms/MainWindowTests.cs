using ValheimServerGUI.Forms;
using Xunit;

namespace ValheimServerGUI.Tests.Forms
{
    public class MainWindowTests : BaseTest
    {
        [Fact]
        public void CanInitMainWindow()
        {
            this.GetForm<MainWindow>();
        }
    }
}
