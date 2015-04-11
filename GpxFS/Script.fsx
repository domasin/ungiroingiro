#I "./bin/Debug"
#r "GpxFS.dll"
#r "System.Runtime"

open System
open System.Collections
open System.Collections.Generic
open GpxFS.GpxXml.Functions
open GpxFS.Geo
open System.IO

let totalLength = 
    use sr = new StreamReader (__SOURCE_DIRECTORY__ + "\\02b - MonteFaie.gpx")
    sr.ReadToEnd()
    |> gpxTracks
    |> List.map (fun (_, locations) -> locations |> List.map (fun (lat,lon,ele,_) -> (lat,lon,ele)))
    |> List.concat
    |> length true


