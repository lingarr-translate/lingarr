﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Lingarr.Core.Entities;

public class Episode : BaseEntity
{
    public required int SonarrId { get; set; }
    public required int EpisodeNumber { get; set; }
    public required string Title { get; set; }
    public string? FileName { get; set; } = string.Empty;
    public string? Path { get; set; } = string.Empty;

    public int SeasonId { get; set; }
    [ForeignKey(nameof(SeasonId))]
    public required Season Season { get; set; }
}