// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Overlays.Login;

namespace osu.Game.Tests.Visual.Menus
{
    [TestFixture]
    public class TestSceneLoginPanel : OsuManualInputManagerTestScene
    {
        private LoginPanel loginPanel;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create login dialog", () =>
            {
                Add(loginPanel = new LoginPanel
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 0.5f,
                });
            });
        }

        [Test]
        public void TestLoginSuccess()
        {
            AddStep("logout", () => API.Logout());

            AddStep("enter password", () => loginPanel.ChildrenOfType<OsuPasswordTextBox>().First().Text = "password");
            AddStep("submit", () => loginPanel.ChildrenOfType<OsuButton>().First(b => b.Text.ToString() == "Sign in").TriggerClick());
        }

        [Test]
        public void TestLoginFailure()
        {
            AddStep("logout", () =>
            {
                API.Logout();
                ((DummyAPIAccess)API).FailNextLogin();
            });

            AddStep("enter password", () => loginPanel.ChildrenOfType<OsuPasswordTextBox>().First().Text = "password");
            AddStep("submit", () => loginPanel.ChildrenOfType<OsuButton>().First(b => b.Text.ToString() == "Sign in").TriggerClick());
        }
    }
}
