module GpxFS.Geo

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

//let test = 
//    [
//        46.0928883589804,8.47813253290951,1243.;
//        46.0929166059941,8.47807151265442,1253.;
//        46.0929222218692,8.47801174968481,1265.;
//        46.0928777139634,8.47801468335092,1252.;
//        46.092807976529,8.47801468335092,1245.
//    ]
//
//length true test