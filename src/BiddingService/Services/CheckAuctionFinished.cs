
using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services
{
    public class CheckAuctionFinished(ILogger<CheckAuctionFinished> logger, IServiceProvider service) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting check for finished auctions");

            stoppingToken.Register(() => logger.LogInformation("==> Auction check is stopping"));

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAuction(stoppingToken);

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task CheckAuction(CancellationToken stoppingToken)
        {
            var finishedAuction = await DB.Find<Auction>()
                .Match(x => x.AuctionEnd <= DateTime.UtcNow)
                .Match(x => !x.Finished)
                .ExecuteAsync();

            if (finishedAuction.Count == 0) return;

            logger.LogInformation("==> Found {count} auctions that have completed", finishedAuction.Count);

            using var scope = service.CreateScope();
            var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            foreach (var auction in finishedAuction)
            {
                auction.Finished = true;
                await auction.SaveAsync(null, stoppingToken);

                var winningBid = await DB.Find<Bid>()
                    .Match(a => a.AuctionId == auction.ID)
                    .Match(b => b.BidStatus == BidStatus.Accepted)
                    .Sort(x => x.Descending(s => s.Amount))
                    .ExecuteFirstAsync();

                await endpoint.Publish(new AuctionFinished
                {
                    ItemSold = winningBid != null,
                    AuctionId = auction.ID,
                    Winner = winningBid?.Bidder,
                    Amount = winningBid?.Amount,
                    Seller = auction.Seller
                }, stoppingToken);
            }
        }
    }
}
