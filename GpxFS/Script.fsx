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
    use sr = new StreamReader (__SOURCE_DIRECTORY__ + "\\20150412-2.gpx")
    sr.ReadToEnd()
    |> gpxTracks
    |> List.map (fun (_, locations) -> locations |> List.map (fun (lat,lon,ele,_) -> (lat,lon,ele)))
    |> List.concat
    |> length true

distance (46.0928883589804,8.47813253290951,1243.) (46.085941856727,8.47988535650074,1279.) true;;
//784.4509921
//784.4509921465799

length true [(46.0928883589804,8.47813253290951,1243.);(46.085941856727,8.47988535650074,1279.)]


