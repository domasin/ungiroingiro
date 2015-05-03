module GpxFS.GpxXml.Functions

open System
open System.Xml.Linq
open System.Globalization

let ofSeq (arr: 'T seq) = new System.Collections.Generic.List<'T>(arr)

let gpxDoc content = XDocument.Parse(content)

let gpxNameSpace = XNamespace.Get("http://www.topografix.com/GPX/1/1")

let gpxWaypoints content = 
    let gpxDoc = gpxDoc content
    gpxDoc.Descendants(gpxNameSpace + "wpt")
    |> Seq.map 
        (fun wpt -> 
            (wpt.Attribute(XName.Get("lat")).Value |> double),
            (wpt.Attribute(XName.Get("lon")).Value |> double), 
            (
                let ele = wpt.Element(gpxNameSpace + "ele")
                if ele <> null then ele.Value |> double else Double.NaN
            ), 
            (   
                let name = wpt.Element(gpxNameSpace + "name")
                if name <> null then name.Value else ""
            ),
            (   
                let cmt = wpt.Element(gpxNameSpace + "cmt")
                if cmt <> null then cmt.Value else ""
            )
        )
    |> Seq.toList

let gpxTracks content = 
    let gpxDoc = gpxDoc content
    gpxDoc.Descendants(gpxNameSpace + "trk")
    |> Seq.map 
        (fun trk -> 
            (let name = trk.Element(gpxNameSpace + "name")
             if name <> null then name.Value else ""), 
            trk.Descendants(gpxNameSpace + "trkseg")
            |> Seq.map 
                (fun trkseg -> 
                    trk.Descendants(gpxNameSpace + "trkpt")
                    |> Seq.map 
                        (fun trkpt -> 
                            (trkpt.Attribute(XName.Get("lat")).Value |> double),
                            (trkpt.Attribute(XName.Get("lon")).Value |> double), 
                            (   
                                let ele = trkpt.Element(gpxNameSpace + "ele")
                                if ele <> null then ele.Value |> double else Double.NaN
                            ), 
                            (   
                                let time = trkpt.Element(gpxNameSpace + "time")
                                if time <> null then DateTime.Parse(time.Value) else DateTime.MinValue
                            )
                        )
                    |> Seq.toList 
                )
                |> Seq.toList 
        )
    |> Seq.toList

let gpxRoutes content = 
    let gpxDoc = gpxDoc content
    gpxDoc.Descendants(gpxNameSpace + "rte")
    |> Seq.map 
        (fun rte -> 
            (let name = rte.Element(gpxNameSpace + "name")
             if name <> null then name.Value else ""), 
            rte.Descendants(gpxNameSpace + "rtept")
            |> Seq.map 
                (fun rtept -> 
                    (rtept.Attribute(XName.Get("lat")).Value |> double),
                    (rtept.Attribute(XName.Get("lon")).Value |> double)
                )
            |> Seq.toList
        )
    |> Seq.toList

