using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = "servidor", menuName = "proyecto/servidor", order = 1)]
public class Servidor : ScriptableObject{
    public string nombreServidor;
    public servicio[] servicios;
    public bool ocupado;
    public Respuesta respuesta;

    public IEnumerator consumir_servicio(string nombre, string[] datos,Action<Respuesta> onCompletado){

        ocupado= true;
        WWWForm formulario = new WWWForm();
        servicio s = new servicio();
        for (int i = 0; i < servicios.Length; i++){
            if(servicios[i].nombre.Equals(nombre)){
                s = servicios[i];
            }
        }
        for (int j = 0; j < s.parametros.Length; j++){
                    formulario.AddField(s.parametros[j], datos[j]);
        }
        UnityWebRequest www = UnityWebRequest.Post(nombreServidor + "/" + s.URL,formulario);
        Debug.Log(nombreServidor + "/" + s.URL);
        yield return www.SendWebRequest();
        if(www.result != UnityWebRequest.Result.Success){
            respuesta =  new Respuesta();
        }else{
            Debug.Log(www.downloadHandler.text);
            respuesta = JsonUtility.FromJson<Respuesta>(www.downloadHandler.text);
        }
        ocupado = false;
        onCompletado?.Invoke(respuesta);
    }

}

[System.Serializable]

public class servicio{
    public string nombre;
    public string URL;
    public string[] parametros;
}

[System.Serializable]

public class Respuesta{
    public int codigo;
    public string mensaje;
    public Respuesta(){
        codigo = 404;
        mensaje = "Error";
    }
}