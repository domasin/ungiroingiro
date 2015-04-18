// Experimental porting from https://github.com/tkrajina/gpxpy/blob/master/gpxpy/geo.py

module GpxFS.Geo

open System

type location = (float * float * float)

let ONE_DEGREE = 1000. * 10000.8 / 90.

let EARTH_RADIUS = 6371. * 1000.

let pi = System.Math.PI
let to_rad x = x / 180. * pi

/// Haversine distance between two points, expressed in meters.
/// Implemented from http://www.movable-type.co.uk/scripts/latlong.html
/// Formula dell'Emisenoverso
let haversine_distance (lat1,lon1) (lat2,lon2) = 
    let d_lat = to_rad (lat1 - lat2)
    let d_lon = to_rad (lon1 - lon2)
    let lat1 = to_rad (lat1)
    let lat2 = to_rad (lat2)

    let a = sin(d_lat / 2.) * sin(d_lat / 2.) + 
                sin(d_lon / 2.) * sin(d_lon / 2.) * cos(lat1) * cos(lat2)
    let c = 2. * (atan2 (sqrt(a)) (sqrt(1. - a)))
    let d = EARTH_RADIUS * c

    d

/// Distance between two points. If elevation is 0, compute a 2d distance
/// 
/// if haversine==true -- haversine will be used for every computations,
/// otherwise...
/// 
/// Haversine distance will be used for distant points where elevation makes a
/// small difference, so it is ignored. That's because haversine is 5-6 times
/// slower than the dummy distance algorithm (which is OK for most GPS tracks).
let distance (lat1, lon1, elev1) (lat2, lon2, elev2) haversine = 
    
    // If points too distant -- compute haversine distance:
    if haversine || (abs(lat1 - lat2) > 0.2 || abs(lon1 - lon2) > 0.2) then 
        haversine_distance (lat1,lon1) (lat2,lon2)
    else
        let coef = cos(lat1 / 180. * pi)
        let x = lat1 - lat2
        let y = (lon1 - lon2) * coef

        let distance_2d = sqrt(x * x + y * y) * ONE_DEGREE

        if elev1 = 0. || elev2 = 0. || elev1 = elev2 then
            distance_2d
        else
            sqrt(distance_2d ** 2. + (elev1 - elev2) ** 2.)

let distance_2d (lat1, lon1, _) (lat2, lon2, _) = 
    let loc1 = (lat1, lon1, 0.)
    let loc2 = (lat2, lon2, 0.)
    distance loc1 loc2 false

let distance_3d loc1 loc2 = distance loc1 loc2 false

let length _3d locations = 
    let rec len distFunc acc locations = 
        match locations with
        | loc1::loc2::x -> 
            let acc = (distFunc loc1 loc2) + acc
            len distFunc acc (loc2::x )
        | _ -> acc
    locations |> len (if _3d then distance_3d else distance_2d) 0.

/// 2-dimensional length (meters) of locations (only latitude and longitude, no elevation).
let length_2d = length false

/// 3-dimensional length (meters) of locations (it uses latitude, longitude, and elevation).
let length_3d = length true

/// Compute average distance and standard deviation for distance. Extremes
/// in distances are usually extremes in speeds, so we will ignore them,
/// here.
/// 
/// speeds_and_distances must be a list containing pairs of (speed, distance)
/// for every point in a track segment.
let calculate_max_speed speeds_and_distances = 
    let size = speeds_and_distances |> List.length
//    if size < 20 then printfn "Segment too small to compute speed, size=%i" size
    let distances = speeds_and_distances |> List.map (fun (speed,distance) -> distance)
    let average_distance = 
        let sum_distances = distances |> List.fold (fun acc x -> x + acc) 0.
        sum_distances / (size |> float)
    let standard_distance_deviation = 
        let sum = 
            distances
            |> List.map (fun distance -> (distance - average_distance) ** 2.)
            |> List.fold (fun acc x -> x + acc) 0.
        sqrt(sum / (size |> float))

    // Ignore items where the distance is too big:
    let filtered_speeds_and_distances = 
        speeds_and_distances
        |> List.filter 
            (fun (speed,distance) -> 
                let t1 = abs(distance - average_distance)
                let t2 = standard_distance_deviation * 1.5
                t1 <= t2
             )

    // sort by speed:
    let speeds = 
        filtered_speeds_and_distances 
        |> List.map (fun (speed,distance) -> speed)
        |> List.sortBy (fun sp -> sp)

    // Even here there may be some extremes => ignore the last 5%:
    let index = (((speeds |> List.length) |> float) * 0.95) |> int

    speeds |> Seq.ofList |> Seq.nth index
    
let calculate_uphill_downhill elevations = 
    let rec calc (uphill, downhill) elevs = 
        match elevs with
        | elev1::elev2::x -> 
            let delta = elev2 - elev1
            let uphill = uphill + (max 0. delta)
            let downhill = downhill - (min 0. delta)
            calc (uphill, downhill) (elev2::x)
        | _ -> (uphill, downhill)
    calc (0., 0.) elevations

/// Uphill/downhill angle between two locations.
let elevation_angle loc1 loc2 radians = 
    let ((_,_,elev1),(_,_,elev2)) = (loc1, loc2)
    if elev1 = 0. || elev2 = 0. then 0. else 
        let deltaElev = elev2 - elev1
        let distance2d = distance_2d loc1 loc2
        if distance2d = 0. then 0. else
            let angle = atan (deltaElev / distance2d)
            if radians then angle else
                180. * angle / pi

/// Distance of point from a line given with two points.
let distance_from_line point line_point_1 line_point_2 = 
    let a = distance_2d line_point_1 line_point_2
    let b = distance_2d line_point_1 point
    if a = 0. then b else 
        let c = distance_2d line_point_2 point
        let s = (a + b + c) / 2.
        2. * sqrt(abs(s * (s - a) * (s - b) * (s - c))) / a

/// Get line equation coefficients for:
///         latitude * a + longitude * b + c = 0
/// 
///     This is a normal cartesian line (not spherical!)
let get_line_equation_coefficients (lat1,lon1,elev1) (lat2,lon2,elev2) = 
    if lon1 = lon2 then
        // vertical line
        (0., 1., lon1)
    else
        let a = (lat1 - lat2) / (lon1 - lon2)
        let b = lat1 - lon1 * a
        (1., -a, -b)

///// Does Ramer-Douglas-Peucker algorithm for simplification of polyline
//let simplify_polyline points max_distance = 
//    if points |> List.length < 3 then
//        0.
//    else
//        let first,last = points.[0], points.[points.Length - 1]
//
//        // Use a "normal" line just to detect the most distant point (not its real distance)
//        // this is because this is faster to compute than calling distance_from_line() for
//        // every point.
//        //
//        // This is an approximation and may have some errors near the poles and if
//        // the points are too distant, but it should be good enough for most use
//        // cases...
//        let a, b, c = get_line_equation_coefficients first last
//
//        let tmp_max_distance = -1000000.