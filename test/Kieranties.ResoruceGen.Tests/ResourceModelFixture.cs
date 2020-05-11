// Licensed under the MIT license. See https://kieranties.mit-license.org/ for full license information.

using System;
using FluentAssertions;
using Kieranties.ResourceGen;
using Xunit;

namespace Kieranties.ResoruceGen.Tests
{
    public class ResourceModelFixture
    {
        [Fact]
        public void Ctor_NullSource_Throws()
        {
            Action action = () => new ResourceModel(null);

            action.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("source");
        }
    }
}
