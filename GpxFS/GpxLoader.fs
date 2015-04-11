namespace GpxFS

open System
open GpxFS.GpxXml.Functions
open System.Collections.Generic

type GpxPoint(lat:double, lon:double, ele:double, time:DateTime) = 
    member x.Latitude = lat
    member x.Longitude = lon
    member x.Elevation = ele
    member x.Time = time

type GpxTrack(name:string, segs:(List<GpxPoint>)) = 
    member x.Name = name
    member x.Segs = segs

type GpxLoader(content) = 

    member this.GetTracks() = 
        content
        |> gpxTracks
        |> Seq.map 
            (fun (name,segs) -> 
                let points = 
                    segs
                    |> List.map (fun (lat,lon,ele,time) -> new GpxPoint(lat,lon,ele,time))
                new GpxTrack(name,points |> ofSeq)
            )
        |> ofSeq

    member this.GetRoutes() = 
        content
        |> gpxRoutes
        |> Seq.map 
            (fun (name,segs) -> 
                let points = 
                    segs
                    |> List.map (fun (lat,lon) -> new GpxPoint(lat,lon,0.,DateTime.MinValue))
                new GpxTrack(name,points |> ofSeq)
            )
        |> ofSeq
