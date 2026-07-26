namespace PropertySalesMVC.Models
{
    // Consolidates the previous LocationMaster / LocationMasterNew / LocationViewModel
    // shapes. Views access either .LocationName (AddProperty/EditProperty) or
    // .Location (Listing/_PropertyFilter/Sale) via dynamic ViewBag binding — this
    // single type satisfies both.
    public class LocationOption
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string LocationName => Location;
        public bool IsActive { get; set; } = true;
    }
}
