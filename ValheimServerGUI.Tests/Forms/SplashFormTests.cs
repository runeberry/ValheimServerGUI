using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            this.GetForm<SplashForm>();
        }
    }
}
