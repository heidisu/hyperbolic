module Hyperbolic

open System
open System.Drawing

open FsAlg.Generic

type Point = Point of float * float
type Action = Draw of Point * Point
type Instructions = Action list
type Adjacency = Vertex | Edge
type ImageProperties = 
        { Size : int
          CircleRadius : float }

let imageProperties = { Size = 700
                        CircleRadius = 300.0 }
let [<Literal>] P = 6
let [<Literal>] Q = 4

let fP = float P
let fQ = float Q
let coshB = Math.Cos(Math.PI/fQ) / Math.Sin(Math.PI/fP)
let cosh2B = 2.0 * Math.Pow(coshB, 2.0) - 1.0
let sinh2B = Math.Sqrt(Math.Pow(cosh2B, 2.0)-1.0)
let identity = matrix [[1.0; 0.0; 0.0]
                       [0.0; 1.0; 0.0]
                       [0.0; 0.0; 1.0]]
let reflectEdgeBisector = matrix [[1.0; 0.0; 0.0]
                                  [0.0; -1.0; 0.0]
                                  [0.0; 0.0; 1.0]]
let reflectPgonEdge = matrix [[-cosh2B; 0.0;sinh2B]
                              [0.0; 1.0; 0.0]
                              [-sinh2B; 0.0; cosh2B]]
let reflectHypotenuse = matrix [[Math.Cos(2.0*Math.PI/fP); Math.Sin(2.0*Math.PI/fP); 0.0]
                                [Math.Sin(2.0*Math.PI/fP); -Math.Cos(2.0*Math.PI/fP); 0.0]
                                [0.0; 0.0; 1.0]]
let rotateP = reflectHypotenuse * reflectEdgeBisector
let rotateQ = reflectPgonEdge * reflectHypotenuse

let transformPoint (t : Matrix<float>) (Point(x, y)) = 
    let sumSquare = x*x + y*y
    let z = vector [2.0 * x/(1.0 - sumSquare);  2.0 * y/(1.0 - sumSquare); (1.0 + sumSquare)/ (1.0 - sumSquare)]
    let res = t * z
    (res.[0]/(1.0 + res.[2]), res.[1]/(1.0 + res.[2]))

let drawPolygonPattern (graphics : Graphics) (t : Matrix<float>) (instructions : Instructions) = 
    let pen = new Pen(Color.Black)
    let center = (float) imageProperties.Size / 2.0
    let radius = imageProperties.CircleRadius
    let scale (x, y) = (x * radius + center, y * radius + center)
    
    let rectangle = new RectangleF(
                            new PointF(float32 (center - radius), float32 (center - radius)), 
                            new SizeF(PointF(float32 (2.0 * radius), float32 (2.0 * radius))))
    graphics.DrawEllipse(pen, rectangle)
    
    instructions
    |> List.iter (fun instr -> match instr with
                               | Draw (p1, p2) ->   let p1x, p1y = p1 |> transformPoint t |> scale
                                                    let p2x, p2y = p2 |> transformPoint t |> scale
                                                    graphics.DrawLine(pen, new PointF(float32 p1x, float32 p1y), new PointF(float32 p2x, float32 p2y))
                               )

let rec replicate graphics transform layers adjacency instructions = 
    let rotate2P = rotateP * rotateP
    let rotate3P = rotate2P * rotateP

    let rec drawVertexPgon count rotateVertex = 
        if count <= 0 then ()
        else 
           replicate graphics rotateVertex (layers - 1) Vertex instructions
           drawVertexPgon (count - 1) (rotateVertex * rotateQ)

    let rec replicateEdges count rotateCenter = 
        if count <= 0 then ()
        else
            let rotateVertex = rotateCenter * rotateQ
            replicate graphics rotateVertex (layers - 1) Edge instructions
            let vertexPgons = if count > 1 then Q - 3 else Q - 4
            drawVertexPgon vertexPgons (rotateVertex * rotateQ)
            replicateEdges (count - 1) (rotateCenter * rotateP)

    drawPolygonPattern graphics transform instructions
    if layers > 0 then
        match adjacency with
        | Edge -> replicateEdges (P - 3) (transform * rotate3P)
        | Vertex -> replicateEdges (P - 2) (transform * rotate2P)

let drawImage filename layers instructions = 
    let size = imageProperties.Size
    let image = new Bitmap(size, size)
    let graphics = Graphics.FromImage image

    let rec drawQpolygon count rotateVertex = 
        if count = 0 then ()
        else 
            replicate graphics rotateVertex (layers - 2) Vertex instructions
            drawQpolygon (count - 1) (rotateVertex * rotateQ)
    
    let rec drawPpolygon count rotateCenter = 
        if count = 0 then ()
        else
            let rotateVertex = rotateCenter * rotateQ
            replicate graphics rotateVertex (layers - 2) Edge instructions
            drawQpolygon (Q - 3) (rotateVertex * rotateQ)
            drawPpolygon (count - 1) (rotateCenter * rotateP)

    drawPolygonPattern graphics identity instructions
    drawPpolygon P identity  
    image.RotateFlip RotateFlipType.RotateNoneFlipY  
    image.Save filename

let drawWithTesselationLines filename transform instructions = 
    let size = imageProperties.Size
    let image = new Bitmap(size, size)
    let graphics = Graphics.FromImage image
    let center = (float) size/2.0
    let radius = imageProperties.CircleRadius
    
    let linePen = new Pen(Color.Green)
    linePen.DashPattern <-[|float32 4.0; float32 4.0|]
    let circlePen = new Pen(Color.Purple)

  
    let scale (x, y) = new PointF(float32 (x * radius + center), float32 (y * radius + center))

    [1.0 .. fP]
    |> Seq.map (fun n -> new RectangleF(
                                scale(Math.Sqrt(2.0)*Math.Cos(2.0*Math.PI*n/fP) - 1.0, Math.Sqrt(2.0)*Math.Sin(2.0*Math.PI*n/fP) - 1.0),
                                new SizeF (new PointF (float32 (2.0 * radius), float32 (2.0 * radius)))))
    |> Seq.iter (fun rectangle -> graphics.DrawEllipse(circlePen, rectangle))

    [1.0 .. fP]
    |> Seq.iter(fun n -> graphics.DrawLine(
                                        linePen, 
                                        scale(Math.Cos(Math.PI*n/fP), Math.Sin(Math.PI*n/fP)), 
                                        scale(Math.Cos(Math.PI*(n + fP)/fP), Math.Sin(Math.PI*(n + fP)/fP))))

    drawPolygonPattern graphics identity instructions 
    drawPolygonPattern graphics transform instructions 
    image.RotateFlip RotateFlipType.RotateNoneFlipY
    image.Save filename

[<EntryPoint>]
let main _ =
    let c = 0.4
    let d = 0.05
    let rotatePi idx frac c = Point(c * Math.Cos(Math.PI*idx/frac), c * Math.Sin(Math.PI*idx/frac))
    let rotPiP idx = rotatePi idx fP
    let rotPi2P idx = rotatePi idx (2.0 * fP)

    let polygonInstuctions = 
        seq {1.0 .. (2.0 * fP)}
        |> Seq.toList
        |> List.map (fun idx -> Draw(rotPiP idx c, rotPiP (idx + 1.0) c))
    
    let lineInstructions = 
        seq {1.0 .. 2.0 .. (4.0 * fP - 1.0)}
        |> Seq.toList
        |> List.map (fun idx -> Draw(rotPi2P idx d, rotPi2P idx c))

    drawImage "polygons.png" 4 polygonInstuctions
    drawImage "lines.png" 4 lineInstructions

    drawWithTesselationLines "edgebisector.png" reflectEdgeBisector [Draw(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawWithTesselationLines "hypotenuse.png" reflectHypotenuse [Draw(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawWithTesselationLines "pgonedge.png" reflectPgonEdge [Draw(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawWithTesselationLines "rotP.png" rotateP [Draw(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawWithTesselationLines "rotQ.png" rotateQ [Draw(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    
    0
