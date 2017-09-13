using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AzureRepositories.Helpers;
using Microsoft.WindowsAzure.Storage.Table;
using NUnit.Framework;

namespace MarginTradingTests.AzureRepositories.Helpers
{
    [TestFixture]
    public class BatchEntityInsertHelperTests
    {
        [Test]
        public void Always_ShouldCorrectlySplitToBatches()
        {
            //arrange
            string GetPartitionKey(int i)
            {
                if (i < 150)
                {
                    return "1"; // 2 batches
                }
                else if (i < 250)
                {
                    return "2"; // 1 batch
                }
                else if (i < 300)
                {
                    return "3"; // 1 batch
                }
                else
                {
                    return "4"; // 1 batch
                }
            }

            var srcEntities = Enumerable.Range(0, 400)
                .Select(i => new TableEntity(GetPartitionKey(i), ""));

            var resultBatches = new List<List<int>>();

            Task SaveBatchFunc(IReadOnlyCollection<TableEntity> b)
            {
                resultBatches.Add(b.Select(e => int.Parse(e.PartitionKey)).ToList());
                return Task.CompletedTask;
            }

            //act
            BatchEntityInsertHelper.InsertOrReplaceBatchAsync(srcEntities, SaveBatchFunc);

            //assert
            resultBatches.Should().OnlyContain(b => b.Distinct().Count() == 1);
            resultBatches.Should().OnlyContain(b => b.Count <= 100);
            resultBatches.Should().HaveCount(5);
        }


        [Test]
        [TestCase(99, 1, 99, 0)]
        [TestCase(100, 1, 100, 0)]
        [TestCase(101, 2, 100, 1)]
        public void Always_ShouldCorrectlyProcessEnd(int totalEntitiesCount,
            int expectedBatchesCount, int expectedFirstBatchSize, int expectedSecondBatchSize)
        {
            //arrange
            var srcEntities = Enumerable.Range(0, totalEntitiesCount)
                .Select(i => new TableEntity());

            var resultBatchesSizes = new List<int>();

            Task SaveBatchFunc(IReadOnlyCollection<TableEntity> b)
            {
                resultBatchesSizes.Add(b.Count);
                return Task.CompletedTask;
            }

            //act
            BatchEntityInsertHelper.InsertOrReplaceBatchAsync(srcEntities, SaveBatchFunc);

            //assert
            resultBatchesSizes.Should().HaveCount(expectedBatchesCount);
            resultBatchesSizes.FirstOrDefault().Should().Be(expectedFirstBatchSize);
            resultBatchesSizes.Skip(1).FirstOrDefault().Should().Be(expectedSecondBatchSize);
        }
    }
}
