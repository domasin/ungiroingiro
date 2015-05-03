namespace GpxFS

open System
open GpxFS.GpxXml.Functions
open System.Collections.Generic

type GpxPoint(lat:double, lon:double, ele:double, time:DateTime) = 
    member x.Latitude = lat
    member x.Longitude = lon
    member x.Elevation = ele
    member x.Time = time

type GpxRtPoint(lat:double, lon:double) = 
    member x.Latitude = lat
    member x.Longitude = lon

type GpxSeg(points:(List<GpxPoint>)) = 
    member x.Points = points

type GpxTrack(name:string, segs:(List<GpxSeg>)) = 
    member x.Name = name
    member x.Segs = segs

type GpxRoute(name:string, rtepts:(List<GpxRtPoint>)) = 
    member x.Name = name
    member x.Rtepts = rtepts

type GpxLoader(content) = 

    member this.GetTracks() = 
        content
        |> gpxTracks
        |> Seq.map 
            (fun (name,segs) -> 
                let segmenti = 
                    segs
                    |> List.map (fun seg -> 
                                    let points = 
                                        seg
                                        |> List.map (fun (lat,lon,ele,time) -> new GpxPoint(lat,lon,ele,time))
                                    new GpxSeg(points |> ofSeq)
                                )
                new GpxTrack(name,segmenti |> ofSeq)
            )
        |> ofSeq

    member this.GetRoutes() = 
        content
        |> gpxRoutes
        |> Seq.map 
            (fun (name,rtepts) -> 
                let points = 
                    rtepts
                    |> List.map (fun (lat,lon) -> new GpxRtPoint(lat,lon))
                new GpxRoute(name,points |> ofSeq)
            )
        |> ofSeq
