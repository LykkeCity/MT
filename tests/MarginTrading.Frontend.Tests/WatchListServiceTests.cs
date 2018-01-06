using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Frontend.Services;
using NUnit.Framework;

namespace MarginTrading.Frontend.Tests
{
    [TestFixture]
    public class WatchListServiceTests : BaseTests
    {
        private const string ClientId = "client";
        
        private IWatchListService _watchListService;

        [SetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _watchListService = Container.Resolve<IWatchListService>();
        }

        [Test]
        public void Is_AllAssets_Exists()
        {
            var lists = _watchListService.GetAllAsync(ClientId).Result;
            var allAsstsList = lists.FirstOrDefault(item => item.Name == "All assets");

            Assert.IsNotNull(allAsstsList);
            Assert.IsTrue(allAsstsList.ReadOnly);
        }

        [Test]
        public void Is_AllAssets_NotChanged()
        {
            var lists = _watchListService.GetAllAsync(ClientId).Result;
            var allAsstsList = lists.FirstOrDefault(item => item.Name == "All assets");

            Assert.IsNotNull(allAsstsList);

            var result = _watchListService.AddAsync(allAsstsList.Id, ClientId, "New all assets", new List<string> { "EURUSD" }).Result;

            Assert.AreEqual(WatchListStatus.ReadOnly, result.Status);
        }

        [Test]
        public void Is_AllAssets_NotDeleted()
        {
            var lists = _watchListService.GetAllAsync(ClientId).Result;
            var allAsstsList = lists.FirstOrDefault(item => item.Name == "All assets");

            Assert.IsNotNull(allAsstsList);

            var result = _watchListService.DeleteAsync(ClientId, allAsstsList.Id).Result;

            Assert.AreEqual(WatchListStatus.ReadOnly, result.Status);
            Assert.IsFalse(result.Result);

            lists = _watchListService.GetAllAsync(ClientId).Result;
            allAsstsList = lists.FirstOrDefault(item => item.Name == "All assets");

            Assert.IsNotNull(allAsstsList);
        }

        [Test]
        public void Is_WatchList_Added()
        {
            var result = _watchListService.AddAsync(string.Empty, ClientId, "New list", new List<string> { "BTCCHF", "EURUSD" }).Result;
            var lists = _watchListService.GetAllAsync(ClientId).Result;

            Assert.IsNotNull(lists);
            Assert.IsTrue(lists.Count > 0);
            Assert.IsTrue(lists.Any(item => item.Name == "New list"));
            Assert.IsNotNull(result.Result);
            Assert.AreEqual(WatchListStatus.Ok, result.Status);
            Assert.IsTrue(!string.IsNullOrEmpty(result.Result.Id));
            Assert.IsTrue(result.Result.Order > 0);
        }

        [Test]
        public void Is_Wrong_AssetId()
        {
            const string notExistsAssetId = "NOTEXISTS";
            var result = _watchListService.AddAsync(string.Empty, ClientId, "New list", new List<string> { "EURUSD", notExistsAssetId }).Result;

            Assert.AreEqual(WatchListStatus.AssetNotFound, result.Status);
            Assert.AreEqual(notExistsAssetId, result.Message);
            Assert.IsNull(result.Result);
        }

        [Test]
        public void Is_WatchList_Changed()
        {
            var result = _watchListService.AddAsync(string.Empty, ClientId, "New list", new List<string> { "EURUSD" }).Result;

            Assert.NotNull(result.Result);
            Assert.AreEqual(1, result.Result.AssetIds.Count);

            result = _watchListService.AddAsync(result.Result.Id, ClientId, "New list1", new List<string> { "EURUSD", "BTCCHF" }).Result;

            Assert.NotNull(result.Result);
            Assert.AreEqual("New list1", result.Result.Name);
            Assert.AreEqual(2, result.Result.AssetIds.Count);
        }

        [Test]
        public void Is_WatchList_Deleted()
        {
            var result = _watchListService.AddAsync(string.Empty, ClientId, "New list", new List<string> { "EURUSD" }).Result;

            Assert.NotNull(result.Result);
            Assert.AreEqual(1, result.Result.AssetIds.Count);

            var lists = _watchListService.GetAllAsync(ClientId).Result;

            Assert.AreEqual(2, lists.Count);

            var deleteResult = _watchListService.DeleteAsync(ClientId, result.Result.Id).Result;
            lists = _watchListService.GetAllAsync(ClientId).Result;

            Assert.IsTrue(deleteResult.Result);
            Assert.AreEqual(1, lists.Count);
        }
    }
}
