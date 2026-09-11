public readonly struct SegmentContent
{
    public readonly ObsticleSetSO Obsticles;
    public readonly TrackApperenceSO Apperence;

    public SegmentContent(ObsticleSetSO obsticles, TrackApperenceSO apperence)
    {
        Obsticles = obsticles;
        Apperence = apperence;
    }

    public bool IsComplete => Obsticles != null && Apperence != null;

    public SegmentContent WithObsticles(ObsticleSetSO obsticles) => new SegmentContent(obsticles, Apperence);

    public SegmentContent WithApperence(TrackApperenceSO apperence) => new SegmentContent(Obsticles, apperence);
}
