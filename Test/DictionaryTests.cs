using System.Collections.Generic;
using Chronos;
using Xunit;

namespace Test
{

    [Trait("Dictionary Extensions", "Add Only If Doesn't Exist")]
    public class AddOnlyIfDoesntExistTests
    {
        [Fact (DisplayName = "Returns true when a key value pair is added to the dictionary")]
        public void AddsKeyIfDoesntExist()
        {
            var dictionary = new Dictionary<string, string>();

            var result = dictionary.AddOnlyIfDoesntExist("hello", "goodbye");

            Assert.True(result);
            Assert.Equal("goodbye", dictionary["hello"]);
        }

        [Fact (DisplayName = "Returns false when the key already existed ")]
        public void ReturnsFalseWhenKeyIsNotAdded()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"hello", "goodbye"}
            };


            var result = dictionary.AddOnlyIfDoesntExist("hello", "goodbye2");

            Assert.False(result);
            Assert.Equal("goodbye", dictionary["hello"]);           
        }
    }
}