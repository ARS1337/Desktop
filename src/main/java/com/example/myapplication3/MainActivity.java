package com.example.myapplication3;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Button;
import android.widget.SeekBar;

import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.util.Arrays;

public class MainActivity extends AppCompatActivity implements SensorEventListener, View.OnTouchListener {


    private SensorManager sensorManager;
    private Button mgsButton;
    private Button cnnButton;
    private Button bombaButton;
    private Button trimButton;
    private Button driveButton;
    private Button brakeButton;
    //private SeekBar seekbar;
    //private byte seekBarVal=1;

    private final float[] accelerometerReading = new float[3];
    private final float[] magnetometerReading = new float[3];
    private final float[] gyrSpeed = new float[3];

    private final float[] rotationMatrix = new float[9];
    private final float[] orientationAngles = new float[3];


    private final short DIMENSION_HORIZONTAL = 256;//1920;
    private final short DIMENSION_VERTICAL = 256;//1080;

    private final float ANGLE_ROTATION_HORIZONTAL = 100.0f;
    private final float ANGLE_ROTATION_VERTICAL = 100.0f;

    private final float ANGLE_DETECT_HORIZONTAL = ANGLE_ROTATION_HORIZONTAL/ DIMENSION_HORIZONTAL;
    private final float ANGLE_DETECT_VERTICAL = ANGLE_ROTATION_VERTICAL/DIMENSION_VERTICAL;

    private final short MAX_VALUE_HORIZONTAL = DIMENSION_HORIZONTAL/2;
    private final short MAX_VALUE_VERTICAL = DIMENSION_VERTICAL/2;

    byte[] lockValue = new byte[8];
    volatile boolean flag;
    volatile boolean flag1=true;
    //UI Buttons Flags888888888888888888
    volatile boolean engine=false,flaps=false,cannons=false,landingGear=false,machineGun=false,bomb=false,trim=false,brake = false, drive = false;
    //Ui flag button ends

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        sensorManager = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
        mgsButton = (Button)findViewById(R.id.setzero4);
        mgsButton.setOnTouchListener(this);
        cnnButton = (Button)findViewById(R.id.setzero5);
        cnnButton.setOnTouchListener(this);
        bombaButton = (Button)findViewById(R.id.setzero2);
        bombaButton.setOnTouchListener(this);
        trimButton = (Button)findViewById(R.id.setzero3);
        trimButton.setOnTouchListener(this);
        driveButton = (Button)findViewById(R.id.drive);
        driveButton.setOnTouchListener(this);
        brakeButton = (Button)findViewById(R.id.brake);
        brakeButton.setOnTouchListener(this);

        new Thread(new Runnable() {
            @Override
            public void run() {
                short horzValue=0,vertValue=0,rollValue=0,tempHorz=0,tempVert=0,tempRoll=0;
                byte[] movePac = new byte[8];
                DatagramSocket datagramSocket;
                DatagramPacket datagramPacket;
                byte seekBarVal = 1;
                try {
                    datagramSocket = new DatagramSocket();
                while(true){
                    if(flag1){
                    //flag = false;
                    updateOrientationAngles();
                    tempHorz = (short)( 1*((90+Math.toDegrees(orientationAngles[0]))/ANGLE_DETECT_HORIZONTAL));
                    tempVert = (short)(-1* (Math.toDegrees(orientationAngles[2])/ANGLE_DETECT_VERTICAL));
                    tempRoll = (short)(-1* (Math.toDegrees(orientationAngles[1])/ANGLE_DETECT_VERTICAL));
                    if(tempHorz >= -MAX_VALUE_HORIZONTAL && tempHorz <= (MAX_VALUE_HORIZONTAL-1)) horzValue = tempHorz;
                    else {
                        if(tempHorz<=-MAX_VALUE_HORIZONTAL) horzValue = -MAX_VALUE_HORIZONTAL ;
                        else horzValue = MAX_VALUE_HORIZONTAL - 1;
                    }
                        //horzValue = (short) (MAX_VALUE_HORIZONTAL*(tempHorz/ Math.abs(tempHorz)));
                    if(tempVert >= -MAX_VALUE_VERTICAL && tempVert <= (MAX_VALUE_VERTICAL-1)) vertValue = tempVert;
                    else  {
                        if(tempVert<=-MAX_VALUE_VERTICAL) vertValue = -MAX_VALUE_VERTICAL ;
                        else vertValue = MAX_VALUE_VERTICAL - 1;
                    }
                        //vertValue = (short) (MAX_VALUE_VERTICAL*(tempVert/ Math.abs(tempVert)));
                    if(tempRoll >= -MAX_VALUE_VERTICAL && tempRoll <= (MAX_VALUE_VERTICAL-1)) rollValue = tempRoll;
                    else  {
                        if(tempRoll<=-MAX_VALUE_VERTICAL) rollValue = -MAX_VALUE_VERTICAL ;
                        else rollValue = MAX_VALUE_VERTICAL - 1;
                    }
                        //rollValue = (short) (MAX_VALUE_VERTICAL*(tempRoll/ Math.abs(tempRoll)));
                    movePac[0]=(byte) (horzValue&0xff) ;                                 // sWAP xc  with a and yc with b
                    movePac[1]=(byte) ((horzValue>>8)&0xff);                               // same
                    movePac[2]=(byte) (vertValue&0xff);                                    //  same
                    movePac[3]=(byte) ((vertValue>>8)&0xff);                               // same
                    movePac[4]=(byte) (rollValue&0xff);                                    //  same
                    movePac[5]=(byte) ((rollValue>>8)&0xff);
                    movePac[6]=getButtonStatus();
                    seekBarVal = getSeekBarVal();
                    if(!trim) movePac[7]=seekBarVal;
                    else movePac[7]=(byte)(seekBarVal|0b10000000);
                    Log.i("dir", horzValue +" "+ vertValue+" "+rollValue+" "+movePac[6]+" "+machineGun+" "+cannons+" "+ bomb +" "+ trim+" "+movePac[7]) ;
                    datagramPacket= new DatagramPacket(movePac,8,(Inet4Address) InetAddress.getByName("192.168.1.4"),49000);
                    datagramSocket.send(datagramPacket);
                    Thread.sleep(50);
                }}
                } catch (Exception e) { }
            }
        }).start();

        new Thread(new Runnable() {
            @Override
            public void run() {
                //byte[] movePac = new byte[6];
                DatagramSocket datagramSocket;
                DatagramPacket datagramPacket;
                byte seekBarVal = 1;
                //short horzValue=0,vertValue=0,rollValue=0;
                try {
                    datagramSocket = new DatagramSocket();
                    while(true){
                        if(!flag1){
//                           Log.i("dir", horzValue +" "+ vertValue+" "+rollValue) ;
                            lockValue[6]=getButtonStatus();
                            seekBarVal = getSeekBarVal();
                            if(!trim) lockValue[7]=seekBarVal;
                            else lockValue[7]=(byte)(seekBarVal|0b10000000);
                            datagramPacket= new DatagramPacket(lockValue,8,(Inet4Address) InetAddress.getByName("192.168.1.4"),49000);
                            Log.i("dir",lockValue[7]+" ");
                            datagramSocket.send(datagramPacket);
                            Thread.sleep(50);
                        }}
                } catch (Exception e) { }
            }
        }).start();
    }
//UI mappings onClicks88888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
    public void setZeroPress(View view){
        lockValue[0]=(byte) ((short)( 1*((90+Math.toDegrees(orientationAngles[0]))/ANGLE_DETECT_HORIZONTAL))&0xff) ;                                 // sWAP xc  with a and yc with b
        lockValue[1]=(byte) (((short)( 1*((90+Math.toDegrees(orientationAngles[0]))/ANGLE_DETECT_HORIZONTAL))>>8)&0xff);                               // same
        lockValue[2]=(byte) ( (short)(-1* (Math.toDegrees(orientationAngles[2])/ANGLE_DETECT_VERTICAL))&0xff);                                    //  same
        lockValue[3]=(byte) (( (short)(-1* (Math.toDegrees(orientationAngles[2])/ANGLE_DETECT_VERTICAL))>>8)&0xff);                               // same
        lockValue[4]=(byte) ((short)(-1* (Math.toDegrees(orientationAngles[1])/ANGLE_DETECT_VERTICAL))&0xff);                                    //  same
        lockValue[5]=(byte) (((short)(-1* (Math.toDegrees(orientationAngles[1])/ANGLE_DETECT_VERTICAL))>>8)&0xff);
        flag1 = !flag1;
        if(!flag1) ((Button)view).setText("Stable");
        else ((Button)view).setText("No Stable");
    }

    public boolean onTouch(View view, MotionEvent event){
        switch (event.getAction() & MotionEvent.ACTION_MASK){
            case MotionEvent.ACTION_DOWN:
                view.setPressed(true);
                if(view.getId()==R.id.setzero4)machineGun = true;
                if(view.getId()==R.id.setzero5)cannons = true;
                if(view.getId()==R.id.setzero2)bomb = true;
                if(view.getId()==R.id.setzero3)trim = true;
                if(view.getId()==R.id.drive)drive = true;
                if(view.getId()==R.id.brake)brake = true;
                break;
            case MotionEvent.ACTION_UP:
            case MotionEvent.ACTION_OUTSIDE:
            case MotionEvent.ACTION_CANCEL:
                view.setPressed(false);
                if(view.getId()==R.id.setzero4)machineGun = false;
                if(view.getId()==R.id.setzero5)cannons = false;
                if(view.getId()==R.id.setzero2)bomb = false;
                if(view.getId()==R.id.setzero3)trim = false;
                if(view.getId()==R.id.drive)drive = false;
                if(view.getId()==R.id.brake)brake = false;
                break;
            case MotionEvent.ACTION_POINTER_UP:
            case MotionEvent.ACTION_MOVE:
            case MotionEvent.ACTION_POINTER_DOWN:
                break;
        }
        return true;
    }

    public void toggleLookAround(View view){}
    public void toggleAirBrake(View view){}
    public void dropBomb(View view){}

    public void toggleEngine(View view){
        engine=!engine;
        if(!engine) ((Button)view).setText("Engine-set");
        else ((Button)view).setText("Engine-notSet");
    }
    public void toggleFlaps(View view){
        flaps=!flaps;
        if(!flaps) ((Button)view).setText("flap-set");
        else ((Button)view).setText("flap-notSet");
    }
    public void toggleGear(View view){
        landingGear=!landingGear;
        if(!landingGear) ((Button)view).setText("LndGear-set");
        else ((Button)view).setText("LndGer-notSet");
    }

    public void onStartTrackingTouch(SeekBar s){}
    public void onStopTrackingTouch(SeekBar s){}
    public void fireMGs(View view){
//        machineGun=!machineGun;
//        if(!machineGun) ((Button)view).setText("MSG-set");
//        else ((Button)view).setText("MSG-notSet");
    }
    public void fireCannons(View view){
//        cannons=!cannons;
//        if(!cannons) ((Button)view).setText("CANNON-set");
//        else ((Button)view).setText("CANNON-notSet");
    }
    public byte getButtonStatus(){
        byte a = 0b00000000;
        if(engine) a = (byte)(a|0b10000000) ;
        if(flaps) a = (byte)(a|0b01000000);
        if(landingGear) a = (byte)(a|0b00100000);
        if(machineGun) a = (byte)(a|0b00010000);
        if(cannons) a = (byte)(a|0b00001000);
        if(bomb) a = (byte)(a|0b00000100);
        return a;
    }

    public byte getSeekBarVal(){
        if(drive) return 2;
        else if(brake) return 0;
        else return 1;
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
        // Do something here if sensor accuracy changes.
        // You must implement this callback in your code.
    }

    @Override
    protected void onResume() {
        super.onResume();

        // Get updates from the accelerometer and magnetometer at a constant rate.
        // To make batch operations more efficient and reduce power consumption,
        // provide support for delaying updates to the application.
        //
        // In this example, the sensor reporting delay is small enough such that
        // the application receives an update before the system checks the sensor
        // readings again.
        Sensor accelerometer = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
        if (accelerometer != null) {
            sensorManager.registerListener(this, accelerometer,
                    SensorManager.SENSOR_DELAY_GAME, 0);
        }
        Sensor magneticField = sensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD);
        if (magneticField != null) {
            sensorManager.registerListener(this, magneticField,
                    SensorManager.SENSOR_DELAY_GAME, 0);
        }
        Sensor gyroscope = sensorManager.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
        if (accelerometer != null) {
            sensorManager.registerListener(this, gyroscope,
                    SensorManager.SENSOR_DELAY_GAME, 0);

        }
    }

    @Override
    protected void onPause() {
        super.onPause();

        // Don't receive any more updates from either sensor.
        sensorManager.unregisterListener((SensorEventListener) this);
    }

    // Get readings from accelerometer and magnetometer. To simplify calculations,
    // consider storing these readings as unit vectors.
    @Override
    public void onSensorChanged(SensorEvent event) {
        flag = true;
        if (event.sensor.getType() == Sensor.TYPE_ACCELEROMETER) {
            System.arraycopy(event.values, 0, accelerometerReading,
                    0, accelerometerReading.length);
        } else if (event.sensor.getType() == Sensor.TYPE_MAGNETIC_FIELD) {
            System.arraycopy(event.values, 0, magnetometerReading,
                    0, magnetometerReading.length);
        } else if (event.sensor.getType() == Sensor.TYPE_GYROSCOPE) {
            for(int  i = 0; i<  3;i++) gyrSpeed[i] =  (float) Math.toDegrees(event.values[i]);
        }
    }

    // Compute the three orientation angles based on the most recent readings from
    // the device's accelerometer and magnetometer.
    public void updateOrientationAngles() {
        // Update rotation matrix, which is needed to update orientation angles.
        SensorManager.getRotationMatrix(rotationMatrix, null,
                accelerometerReading, magnetometerReading);

        // "mRotationMatrix" now has up-to-date information.

        SensorManager.getOrientation(rotationMatrix, orientationAngles);

        // "mOrientationAngles" now has up-to-date information.
    }
}

