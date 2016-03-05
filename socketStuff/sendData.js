var socket = new WebSocket('ws://10.148.6.9:7569');
var socketOpenFlag = false;
socket.onopen = function() {
    console.log("Socket's open!")
    socketOpenFlag = true;
}
socket.onmessage = function (event) {
    console.log(event.data);
    data = event.data;
    if (id == null){
      id = data;
      console.log('id set to ' + id);
    }
    else {
      data = JSON.parse(data);
      updateCubes(data);
    }
}
socket.onclose = function(){
    scoketOpenFlag = false;
    console.log("Socket's closed :(");
}

function updateCubes (cubes){
  for(var i = 0; i<cubes.length; i++){
    var lst = null;
    if(!cubeList[i]){
      var mesh = new THREE.Mesh( geometry,material );
      mesh.dynamic = true;
      cubeList.push(mesh);
    }
    else{
      var mesh = cubeList[i];
    }
    lst = cubes;//[i];//[id];//.split(',');
    console.log(lst);
    mesh.position.x = lst[0];
    mesh.position.y = lst[1];
    mesh.position.z = lst[2];
    mesh.rotation.x = lst[3];
    mesh.rotation.y = lst[4];
    mesh.rotation.z = lst[5];
  }
  var j = i;
  //for (i; i < cubeList.length; i++ ){
    //scene.remove(cubeList[i]);
  //}
  //cubeList = cubeList.slice(0,j+1);
}
function convertQuat(q1){
  var sqw = q1.w*q1.w;
  var sqx = q1.x*q1.x;
  var sqy = q1.y*q1.y;
  var sqz = q1.z*q1.z;
  var unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
  var test = q1.x*q1.y + q1.z*q1.w;
  var heading, attitude, bank;
  if (test > 0.499*unit) { // singularity at north pole
      heading = 2 * Math.atan2(q1.x,q1.w);
      attitude = Math.PI/2;
      bank = 0;
      return {'x': heading, 'y': attitude, 'z': bank};
  }
  if (test < -0.499*unit) { // singularity at south pole
      heading = -2 * Math.atan2(q1.x,q1.w);
      attitude = -Math.PI/2;
      bank = 0;
      return {'x': heading, 'y': attitude, 'z': bank};
  }
  heading = Math.atan2(2*q1.y*q1.w-2*q1.x*q1.z , sqx - sqy - sqz + sqw);
  attitude = Math.asin(2*test/unit);
  bank = Math.atan2(2*q1.x*q1.w-2*q1.y*q1.z , -sqx + sqy - sqz + sqw)
  return {'x': heading, 'y': attitude, 'z': bank};
}
function sendData(position,quaternion){
  if (!socketOpenFlag || id == null){
    return;
  }
  var orient = convertQuat(quaternion);
  var poseData =
    position.x + ',' +
    position.y + ',' +
    position.z + ',' +
    orient.x   + ',' +
    orient.y   + ',' +
    orient.z;
  var toSend = {
    id: id,
    pose: poseData
  };
  console.log(JSON.stringify(toSend));
  socket.send(JSON.stringify(toSend));
  //socket.send("alskdjf;laskdjfl;askdjfl;askdjfa;lskdjfa;lskdjfa;slkdjfa;lskdjfa;lskdfjas;ldkfjas;ldkfjasd;flkajsdf;lk");
}
