// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public class CloseTextTagOnAutoInsertProviderTest : RazorOnAutoInsertProviderTestBase
    {
        [Fact]
        public void OnTypeCloseAngle_ClosesTextTag()
        {
            RunAutoInsertTest(
input: @"
@{
    <text>$$
}
",
expected: @"
@{
    <text>$0</text>
}
");
        }

        [Fact]
        public void OnTypeCloseAngle_OutsideRazorBlock_DoesNotCloseTextTag()
        {
            RunAutoInsertTest(
input: @"
    <text>$$
",
expected: @"
    <text>
");
        }

        internal override RazorOnAutoInsertProvider CreateProvider()
        {
            var optionsMonitor = new Mock<RazorLSPOptionsMonitor>(MockBehavior.Strict);
            optionsMonitor.SetupGet(o => o.GetCurrentOptions).Returns(RazorLSPOptions.Default);
            var provider = new CloseTextTagOnAutoInsertProvider(optionsMonitor.Object);

            return provider;
        }
    }
}
