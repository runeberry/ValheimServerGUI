using ValheimServerGUI.Forms;
using Xunit;

namespace ValheimServerGUI.Tests.Forms
{
    public class MainWindowTests : BaseTest
    {
        [Fact]
        public void CanConstructMainWindow()
        {
            GetForm<MainWindow>();
        }
    }
}
