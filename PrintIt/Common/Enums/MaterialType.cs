namespace PrintIt.Enums
{
    /// <summary>
    /// Represents the supported material types used for 3D printing and manufacturing.
    /// </summary>
    public enum MaterialType
    {
        // ────────── Plastics ──────────

        /// <summary>Polylactic Acid plastic</summary>
        PLA,

        /// <summary>Acrylonitrile Butadiene Styrene plastic</summary>
        ABS,

        /// <summary>Polyethylene Terephthalate Glycol-modified plastic</summary>
        PETG,

        /// <summary>Nylon (polyamide) plastic</summary>
        Nylon,

        /// <summary>Thermoplastic polyurethane</summary>
        TPU,

        /// <summary>Thermoplastic elastomer</summary>
        TPE,

        /// <summary>Polycarbonate plastic</summary>
        PC,

        /// <summary>Polyether ether ketone plastic</summary>
        PEEK,

        /// <summary>Polyetherimide plastic</summary>
        PEI,

        /// <summary>Acrylonitrile Styrene Acrylate plastic</summary>
        ASA,

        /// <summary>High Impact Polystyrene plastic</summary>
        HIPS,

        /// <summary>Polypropylene plastic</summary>
        PP,

        /// <summary>Water-soluble polyvinyl alcohol support material</summary>
        PVA,

        /// <summary>Water-soluble support material based on polyvinyl alcohol</summary>
        BVOH,

        // ────────── Resins ──────────

        /// <summary>General-purpose photopolymer resin</summary>
        StandardResin,

        /// <summary>Impact-resistant photopolymer resin</summary>
        ToughResin,

        /// <summary>Durable photopolymer resin with low friction</summary>
        DurableResin,

        /// <summary>Flexible photopolymer resin</summary>
        FlexibleResin,

        /// <summary>High temperature-resistant photopolymer resin</summary>
        HighTempResin,

        /// <summary>Burnout-safe resin for investment casting</summary>
        CastableResin,

        /// <summary>Biocompatible resin for dental applications</summary>
        DentalResin,

        /// <summary>Ceramic-filled photopolymer resin</summary>
        CeramicResin,

        /// <summary>ABS-like photopolymer resin with similar mechanical properties</summary>
        ABSLikeResin,

        /// <summary>Water-washable photopolymer resin</summary>
        WaterWashableResin,

        /// <summary>Fast-curing photopolymer resin</summary>
        RapidResin,

        // ────────── Metals ──────────

        /// <summary>Stainless steel alloy</summary>
        StainlessSteel,

        /// <summary>Aluminum alloy</summary>
        Aluminum,

        /// <summary>Titanium alloy</summary>
        Titanium,

        /// <summary>Nickel-based superalloy (Inconel)</summary>
        Inconel,

        /// <summary>Hardened steel used for tooling</summary>
        ToolSteel,

        /// <summary>Pure copper or copper alloy</summary>
        Copper,

        /// <summary>Copper-tin alloy (bronze)</summary>
        Bronze,

        /// <summary>Copper-zinc alloy (brass)</summary>
        Brass,

        /// <summary>Cobalt-chromium alloy</summary>
        CobaltChrome,

        // ────────── Composites ──────────

        /// <summary>PLA reinforced with carbon fiber</summary>
        CarbonFiberPLA,

        /// <summary>Nylon reinforced with carbon fiber</summary>
        CarbonFiberNylon,

        /// <summary>Nylon reinforced with glass fiber</summary>
        GlassFiberNylon,

        /// <summary>PLA blended with wood fibers</summary>
        WoodFillPLA,

        /// <summary>PLA blended with metal powders</summary>
        MetalFillPLA,

        /// <summary>PLA blended with ceramic particles</summary>
        CeramicFillPLA,

        // ────────── Ceramics ──────────

        /// <summary>Ceramic clay material</summary>
        CeramicClay,

        /// <summary>High-density porcelain ceramic</summary>
        Porcelain,

        /// <summary>Stoneware ceramic</summary>
        Stoneware,

        /// <summary>Low-fire earthenware ceramic</summary>
        Earthenware,

        // ────────── Flexible / Rubber-Like ──────────

        /// <summary>Silicone-like flexible photopolymer resin</summary>
        SiliconeLikeResin,

        /// <summary>Soft-grade thermoplastic polyurethane</summary>
        SoftTPU,

        /// <summary>Highly elastic photopolymer resin</summary>
        ElasticResin,

        // ────────── Sand / Industrial ──────────

        /// <summary>Sandstone-based material</summary>
        Sandstone,

        /// <summary>Industrial-grade sand for additive manufacturing</summary>
        IndustrialSand,

        /// <summary>Sand used for metal casting molds</summary>
        CastingSand,

        // ────────── Food / Bio Materials ──────────

        /// <summary>Printable chocolate material</summary>
        Chocolate,

        /// <summary>Printable sugar-based material</summary>
        Sugar,

        /// <summary>Printable dough material</summary>
        Dough,

        /// <summary>Biopolymer gel for experimental or medical printing</summary>
        BiopolymerGel,

        /// <summary>Biological ink for bioprinting applications</summary>
        BioInk
    }
}
