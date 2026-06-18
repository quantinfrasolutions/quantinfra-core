using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Databases.Main.Models.History;

public class ExecutionReportModel : ExecutionReport
{
    public ExecutionReportModel() { }
    
    public ExecutionReportModel(ExecutionReport er) : base(er)
    {
    }
    
    public AccountModel Account { get; init; }
    public AccountModel? BrokerAccount { get; init; }
    // public Event Event { get; init; }
    public Contract Contract { get; init; }
}

public class ExecutionReportConfiguration : IEntityTypeConfiguration<ExecutionReportModel>
{
    public void Configure(EntityTypeBuilder<ExecutionReportModel> builder)
    {
        builder.ToTable("orders_history", "history");
        
        builder.Property(a => a.AccountServiceName).HasColumnName("account_service_name").HasColumnOrder(0).IsRequired();
        builder.Property(a => a.OrderId).HasColumnName("order_id").HasColumnOrder(2).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnOrder(3).IsRequired();
        builder.Property(a => a.ExecId).HasColumnName("exec_id").HasColumnOrder(4).IsRequired();
        builder.Property(a => a.AccountId).HasColumnName("account_id").HasColumnOrder(5).IsRequired();
        builder.Property(a => a.BrokerAccountId).HasColumnName("broker_account_id").HasColumnOrder(6);
        builder.Property(a => a.TransactTime).HasColumnName("transact_time").IsRequired().HasColumnOrder(7);
        builder.Property(a => a.ClOrdId).HasColumnName("cl_ord_id").HasColumnOrder(8);
        builder.Property(a => a.ExternalId).HasColumnName("external_id").HasColumnOrder(9);
        builder.Property(a => a.ExecutionRequestId).HasColumnName("execution_request_id").HasColumnOrder(10);
        builder.Property(a => a.SignalGroupId).HasColumnName("signal_group_id").HasColumnOrder(11);
        builder.Property(a => a.RequestId).HasColumnName("request_id").HasColumnOrder(12);
        builder.Property(a => a.ContractId).HasColumnName("contract_id").IsRequired().HasColumnOrder(13);
        builder.Property(a => a.StrategyPositionId).HasColumnName("strategy_position_id").HasColumnOrder(14);
        builder.Property(a => a.PositionEffect).HasColumnName("position_effect").HasColumnType("text").HasColumnOrder(15);
        builder.Property(a => a.OrdType).HasColumnName("ord_type").HasColumnType("text").IsRequired().HasColumnOrder(16);
        builder.Property(a => a.Side).HasColumnName("side").HasColumnType("text").IsRequired().HasColumnOrder(17);
        builder.Property(a => a.OrderQty).HasColumnName("order_qty").IsRequired().HasColumnOrder(18);
        builder.Property(a => a.Price).HasColumnName("price").HasColumnOrder(19);
        builder.Property(a => a.StopPx).HasColumnName("stop_px").HasColumnOrder(20);
        builder.Property(a => a.OrdStatus).HasColumnName("ord_status").HasColumnType("text").IsRequired().HasColumnOrder(21);
        builder.Property(a => a.ExecType).HasColumnName("exec_type").HasColumnType("text").IsRequired().HasColumnOrder(22);
        builder.Property(a => a.ExecTypeReason).HasColumnName("exec_type_reason").HasColumnType("text").HasColumnOrder(23);
        builder.Property(a => a.CumQty).HasColumnName("cum_qty").IsRequired().HasColumnOrder(24);
        builder.Property(a => a.LeavesQty).HasColumnName("leaves_qty").IsRequired().HasColumnOrder(25);
        builder.Property(a => a.LeavesQty).HasColumnName("leaves_qty").IsRequired().HasColumnOrder(26);
        builder.Property(a => a.LastPx).HasColumnName("last_px").HasColumnOrder(27);
        builder.Property(a => a.LastQty).HasColumnName("last_qty").HasColumnOrder(28);
        builder.Property(a => a.CalculatedCcyLastQty).HasColumnName("calculated_ccy_last_qty").HasColumnOrder(29);
        builder.Property(a => a.TimeInForce).HasColumnName("time_in_force").HasColumnType("text").HasColumnOrder(30);
        builder.Property(a => a.IsSuspended).HasColumnName("is_suspended").IsRequired().HasColumnOrder(31);
        builder.Property(a => a.ActivationDt).HasColumnName("activation_dt").HasColumnOrder(32);
        builder.Property(a => a.ExpireDt).HasColumnName("expire_dt").HasColumnOrder(33);
        builder.Property(a => a.LinkedOrders).HasColumnName("linked_orders").HasColumnType("jsonb").HasColumnOrder(34);
        builder.Property(a => a.TradingSessionsIds).HasColumnName("trading_sessions_ids").HasColumnType("jsonb").HasColumnOrder(35);
        builder.Property(a => a.PegInstructions).HasColumnName("peg_instructions").HasColumnType("jsonb").HasColumnOrder(36);
        builder.Property(a => a.IsVirtual).HasColumnName("is_virtual").IsRequired().HasColumnOrder(37);
        builder.Property(a => a.IsSltp).HasColumnName("is_sltp").IsRequired().HasColumnOrder(38);
        builder.Property(a => a.ExecInst).HasColumnName("exec_inst").HasColumnType("jsonb").HasColumnOrder(39);
        builder.Property(a => a.RejectReason).HasColumnName("reject_reason").HasColumnType("text").HasColumnOrder(40);
        builder.Property(a => a.RejectText).HasColumnName("reject_text").HasColumnOrder(41);
        builder.Property(a => a.ParentPositionId).HasColumnName("parent_position_id").HasColumnOrder(42);
        
        builder.Ignore(a => a.Allocations);
        builder.Ignore(a => a.SignedLastQty);
        
        
        // Exec id is unique only within one AS instance
        builder.HasKey(er => new { er.AccountServiceName, er.ExecId }); 
        
        builder.HasOne(o => o.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(o => o.BrokerAccount)
            .WithMany()
            .HasForeignKey(a => a.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany<Event>()
            .WithOne(e => e.ExecutionReport)
            .HasForeignKey(o => new { o.AccountServiceName, o.ExecId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(o => o.Contract)
            .WithMany()
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(a => new { a.AccountId, a.ExternalId }).AreNullsDistinct(true).IsUnique(false);
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<ExecutionReport>();
    }
}