using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MSTeamsEventsNotifyer.Tests
{
    public class Function1Tests
    {
        public class TheRunMethodTests
        {
            private readonly Mock<ILogger> logger = new Mock<ILogger>();

            [Fact]
            public async void Http_trigger_should_return_help()
            {
                var request = TestFactory.CreateHttpRequest();
                var response = (OkObjectResult)await Function1.Run(request, logger.Object);
                Assert.Equal("This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.", response.Value);
            }

            [Theory]
            [MemberData(nameof(TestFactory.Data), MemberType = typeof(TestFactory))]
            public async void Http_trigger_should_return_known_string_from_member_data(string queryStringKey, string queryStringValue)
            {
                var request = TestFactory.CreateHttpRequest(queryStringKey, queryStringValue);
                var response = (OkObjectResult)await Function1.Run(request, logger.Object);
                Assert.Equal($"Hello, {queryStringValue}. This HTTP triggered function executed successfully.", response.Value);
            }
        }
    }
}