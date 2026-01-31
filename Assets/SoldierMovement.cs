using UnityEngine;
using System.Collections;  // Coroutine-hez

public class SoldierMovement : MonoBehaviour
{
    [SerializeField] private float enemyParallaxX = 4f;  
    [SerializeField] private float enemyParallaxY = 0.2f;  
    [SerializeField] private float moveSpeed = 2f;        
    private float normalizedX;
    private float normalizedY;
    private float waitingPeriod;
    private float startTimer;
    private float targetHeight;
    private bool isMoving = false;
    private float waitForShooting = 4f;
    private float shootingTimer;


    [SerializeField] private float startingHeight = 0f;
    [SerializeField] private float maximumHeight = 3f;
    private int nextAction;
    private bool actionComplete = false;

    [SerializeField] private AudioSource dying;

    void Start(){
        startingHeight = transform.position.y;
        maximumHeight = startingHeight + 3f;
        //DecideAndAct();
    }

    void Update() 
    {   
        /*transform.position = new Vector3(
            normalizedX * enemyParallaxX, // Az X tengelyen történő mozgás
            transform.position.y, // Az Y tengely fix értéken marad
            transform.position.z
        );*/

        
    }

    void DecideAndAct()
    {
        nextAction = DecideAction();
        Debug.Log("Next Action: " + nextAction);
        if(nextAction == 0){
            // Akció 0: Leguggolva marad, majd újra dönt
            StartCoroutine(StayCrouchedAndDecideAgain());
        }else if(nextAction == 1){
            // Akció 1: Félig előbújik, majd visszamegy és újra dönt
            targetHeight = startingHeight + (maximumHeight - startingHeight) / 2f; // Pontosítva a targetHeight
            Debug.Log("PeekAndReturn Target Height: " + targetHeight);
            StartCoroutine(PeekAndReturn());  
        }else if(nextAction == 2){
            // Akció 2: Teljesen előbújik, majd visszamegy és újra dönt
            targetHeight = maximumHeight; // Pontosítva a targetHeight
            Debug.Log("FullyEmergeAndReturn Target Height: " + targetHeight);
            StartCoroutine(FullyEmergeAndReturn());
        }
    }

    int DecideAction(){
        return Random.Range(0, 3); // Három akció közül választ
    }

    IEnumerator StayCrouchedAndDecideAgain()
    {
        yield return new WaitForSeconds(Random.Range(2f, 5f));
        DecideAndAct();
    }

    IEnumerator PeekAndReturn()
    {
        Debug.Log("Starting PeekAndReturn with Target Height: " + targetHeight);
        isMoving = true;
        while (transform.position.y < targetHeight) {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position += Vector3.up * moveSpeed * deltaTime;
            Debug.Log("Moving Up - Current Height: " + transform.position.y);
            yield return null;
        }
        Debug.Log("Reached Target Height in PeekAndReturn: " + transform.position.y);
        yield return new WaitForSeconds(2f); // Rövid időt marad félig előbújva

        while (Mathf.Abs(transform.position.y - startingHeight) > 0.01f) { // Tolerancia hozzáadása
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position -= Vector3.up * moveSpeed * deltaTime;
            Debug.Log("Moving Down - Current Height: " + transform.position.y);
            yield return new WaitForEndOfFrame();
        }
        transform.position = new Vector3(transform.position.x, startingHeight, transform.position.z); // Biztosítjuk, hogy pontosan az alaphelyzetbe kerüljön
        isMoving = false;
        yield return new WaitForSeconds(1f); // Visszatérés után vár
        Debug.Log("Finished PeekAndReturn, Deciding Next Action");
        DecideAndAct();
    }

    IEnumerator FullyEmergeAndReturn()
    {
        Debug.Log("Starting FullyEmergeAndReturn with Target Height: " + targetHeight);
        isMoving = true;
        while (transform.position.y < targetHeight) {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position += Vector3.up * moveSpeed * deltaTime;
            Debug.Log("Moving Up - Current Height: " + transform.position.y);
            yield return null;
        }
        Debug.Log("Reached Target Height in FullyEmergeAndReturn: " + transform.position.y);
        yield return new WaitForSeconds(4f); // Teljesen előbújva marad egy ideig

        while (Mathf.Abs(transform.position.y - startingHeight) > 0.01f) { // Tolerancia hozzáadása
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position -= Vector3.up * moveSpeed * deltaTime;
            Debug.Log("Moving Down - Current Height: " + transform.position.y);
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, startingHeight, transform.position.z); // Biztosítjuk, hogy pontosan az alaphelyzetbe kerüljön
        isMoving = false;
        yield return new WaitForSeconds(2f); // Visszatérés után vár
        Debug.Log("Finished FullyEmergeAndReturn, Deciding Next Action");
        DecideAndAct();
    }

    public void SetNormalized(float x, float y){
        normalizedX = x;
        normalizedY = y;
    }

    public void Die(){
        dying.Play();
        Destroy(gameObject, 2f);
    }
}
