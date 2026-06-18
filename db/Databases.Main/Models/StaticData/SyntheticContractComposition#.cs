// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
// using Microsoft.EntityFrameworkCore;
// using NodaTime;
//
// namespace Databases.Main.Models.Contracts;
//
// [Table("synth_composition_history")]
// public class SyntheticContractComposition
// {
//     [Column("composition_id")] [Required] [Key] public long CompositionId { get; set; }
//     [Column("valid_from")] public Instant? ValidFrom { get; set; }
//     [Column("contract_id")] public long ContractId { get; init; }
//     public virtual Contract Contract { get; set; } = default!;
//     
//     [Column("initial_price")] public double? InitialPrice { get; set; }
//     public ICollection<CompositionWeight> CompositionWeights { get; set; } = default!;
//
//     /// <summary>
//     /// To call this method, CompositionWeights must be included into the query
//     /// </summary>
//     /// <returns></returns>
//     public Common.StaticData.Synthetics.SyntheticContractComposition ToComposition() => new()
//     {
//         ValidFrom = ValidFrom,
//         InitialPrice = InitialPrice,
//         Weights = CompositionWeights.ToDictionary(w => w.ContractId, w => w.Weight),
//         InitialPrices = CompositionWeights.Where(w => w.InitialPrice.HasValue)
//             .ToDictionary(w => w.ContractId, w => w.InitialPrice!.Value)
//     };
//     
//
//     public static void CreateRelations(ModelBuilder modelBuilder)
//     {
//         modelBuilder.Entity<SyntheticContractComposition>()
//             .HasIndex(sc => new { sc.ContractId, sc.ValidFrom })
//             .IsUnique();
//         
//         modelBuilder.Entity<SyntheticContractComposition>()
//             .HasOne(c => c.Contract)
//             .WithMany(c => c.SyntheticCompositionHistory)
//             .HasForeignKey(c => c.ContractId)
//             .OnDelete(DeleteBehavior.Restrict);
//     }
// }