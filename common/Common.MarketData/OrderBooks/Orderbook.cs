using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData.OrderBooks;

public sealed class OrderBookSide : IOrderBookSide
{
    private readonly BookSide _side;
    private readonly List<BookLevel> _levels;
    private readonly Dictionary<decimal, int> _priceToIndex;

    public OrderBookSide(BookSide side, int capacity = 256)
    {
        _side = side;
        _levels = new(capacity);
        _priceToIndex = new(capacity);
    }

    public int Count => _levels.Count;

    public IReadOnlyList<BookLevel> Levels => _levels;

    public bool TryGetBest(out BookLevel level)
    {
        if (_levels.Count == 0)
        {
            level = default;
            return false;
        }

        level = _levels[0];

        return true;
    }
    
    public BookLevel? GetBest() => TryGetBest(out var best) ? best : null;

    public bool TryGetLevelByPrice(decimal priceTicks, out BookLevel level, out int levelNumber)
    {
        if (!_priceToIndex.TryGetValue(priceTicks, out var index))
        {
            level = default;
            levelNumber = -1;
            return false;
        }

        level = _levels[index];
        levelNumber = index;
        return true;
    }

    public bool TryGetLevelByNumber(int levelNumber, out BookLevel level)
    {
        if ((uint)levelNumber >= (uint)_levels.Count)
        {
            level = default;
            return false;
        }

        level = _levels[levelNumber];
        return true;
    }

    internal void Update(decimal priceTicks, decimal quantity)
    {
        if (quantity <= 0)
        {
            Remove(priceTicks);
            return;
        }

        if (_priceToIndex.TryGetValue(priceTicks, out var existingIndex))
        {
            _levels[existingIndex] = new BookLevel(priceTicks, quantity);
            return;
        }
        
        Insert(priceTicks, quantity);
    }

    private bool Remove(decimal priceTicks)
    {
        if (!_priceToIndex.TryGetValue(priceTicks, out var index))
            return false;

        _levels.RemoveAt(index);
        _priceToIndex.Remove(priceTicks);
        ReindexFrom(index);

        return true;
    }

    public void Clear()
    {
        _levels.Clear();
        _priceToIndex.Clear();
    }

    private void Insert(decimal priceTicks, decimal quantity)
    {
        var index = FindInsertIndex(priceTicks);
        _levels.Insert(index, new BookLevel(priceTicks, quantity));
        ReindexFrom(index);
    }

    private int FindInsertIndex(decimal priceTicks)
    {
        var lo = 0;
        var hi = _levels.Count;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var midPrice = _levels[mid].Price;
            var shouldGoLeft = _side == BookSide.Bid
                ? priceTicks > midPrice   // bids: higher price is better
                : priceTicks < midPrice;  // asks: lower price is better

            if (shouldGoLeft)
                hi = mid;

            else
                lo = mid + 1;
        }

        return lo;
    }

    private void ReindexFrom(int start)
    {
        for (var i = start; i < _levels.Count; i++)
            _priceToIndex[_levels[i].Price] = i;
    }
}

public sealed class OrderBook : IOrderBook
{
    private readonly int _contractId;
    private readonly OrderBookSide _bids, _asks;
    public IOrderBookSide Bids => _bids;
    public IOrderBookSide Asks => _asks;
    public Instant LastUpdate { get; private set; } = Instant.MinValue;
    
    public OrderBook(int contractId, int capacity = 256)
    {
        _contractId = contractId;
        _bids = new OrderBookSide(BookSide.Bid, capacity);
        _asks = new OrderBookSide(BookSide.Ask, capacity);
    }

    public OrderBook(OrderBookSnapshot snapshot) : this(snapshot.ContractId,
        Math.Max(snapshot.Bids.Count, snapshot.Asks.Count))
    {
        foreach (var bid in snapshot.Bids)
        {
            Update(BookSide.Bid, bid.Price, bid.Quantity, snapshot.Timestamp);
        }
        foreach (var ask in snapshot.Asks)
        {
            Update(BookSide.Ask, ask.Price, ask.Quantity, snapshot.Timestamp);
        }
    }
    
    public OrderBookSnapshot GetSnapshot() => new(_contractId, LastUpdate, Bids.Levels.ToList(), Asks.Levels.ToList());

    public void Update(BookSide side, decimal priceTicks, decimal quantity, Instant ts)
    {
        (side == BookSide.Bid ? _bids : _asks).Update(priceTicks, quantity);
        LastUpdate = ts;
    }
}