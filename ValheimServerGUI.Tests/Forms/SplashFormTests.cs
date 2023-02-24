using ValheimServerGUI.Forms;
using Xunit;

namespace ValheimServerGUI.Tests.Forms
{
    public class SplashFormTests : BaseTest
    {
        public SplashFormTests()
        {
        }

        [Fact]
        public void CanConstructSplashForm()
        {
            GetForm<SplashForm>();
        }
    }
}
