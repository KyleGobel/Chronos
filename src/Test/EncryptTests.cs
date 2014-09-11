using Chronos;
using Xunit;

namespace Test
{
    [Trait("Encrypt", "")]
    public class EncryptTests
    {
        [Fact(DisplayName = "Correctly Sha-1 encodes a string")]
        public void CanSha1Hash()
        {
            const string testString = "i am not one of you";

            const string expectedOutput = "3c787806e6aadb6433b491341f62a523bcca1488";
            var actualOutput = Encrypt.Sha1(testString);

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact(DisplayName = "Correctly Sha-256 encodes a string")]
        public void CanSha256Hash()
        {
            const string testString = "but i fight!";
            const string expectedOutput = "6a83f785edda302643eb3224f18e773fc3544ebf283ec0d9327f32201a054ba5";

            var actualOutput = Encrypt.Sha256(testString);

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact(DisplayName = "Correctly Md5 encodes a string")]
        public void CanMd5Hash()
        {
            //name the movie
            const string testString = "I fight with _____!";

            const string expectedOutput = "d8918ffc4eff89045c799e768f645c32";

            var actualOutput = Encrypt.Md5(testString);

            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}